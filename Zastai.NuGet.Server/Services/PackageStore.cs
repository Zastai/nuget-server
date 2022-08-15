using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.XPath;

using Microsoft.AspNetCore.Mvc;

namespace Zastai.NuGet.Server.Services;

/// <summary>Utilities for working with the package store.</summary>
public class PackageStore : IPackageStore {

  /// <summary>Creates a new package store.</summary>
  /// <param name="logger">A logger for the package store.</param>
  /// <param name="host">The host environment.</param>
  /// <param name="symbolStore">The symbol store to use for PDB files from symbol packages.</param>
  public PackageStore(ILogger<PackageStore> logger, IHostEnvironment host, ISymbolStore symbolStore) {
    this._logger = logger;
    this._packageFolder = Path.Combine(host.ContentRootPath, "data", "packages");
    this._symbolStore = symbolStore;
    this._tempFolder = Path.Combine(host.ContentRootPath, "data", "temp", "publish");
  }

  #region IPackageStore

  /// <inheritdoc />
  public async Task<IActionResult> AddPackageAsync(IFormFile file, string owner, CancellationToken cancellationToken) {
    return await this.WithTempFile<IActionResult>(file, async (tempFile, packageStream) => {
      var info = await this.GetPackageManifestInfo(packageStream, false);
      if (info is null) {
        return new BadRequestResult();
      }
      var (id, version, nuspec) = info.Value;
      // TODO: Maybe validate the owner?
      var packageDir = Path.Combine(this.PackageFolder, id, version);
      // FIXME: Or should this check for the package file and/or nuspec?
      if (Directory.Exists(packageDir)) {
        return new ConflictResult();
      }
      if (this.StorePackageFile(tempFile, packageDir, id, version, Constants.PackageExtension) is null) {
        return new ConflictResult();
      }
      { // Store the manifest in a separate file too
        var nuspecPath = Path.Combine(packageDir, Constants.SpecExtension);
        File.Delete(nuspecPath);
        await File.WriteAllTextAsync(nuspecPath, nuspec, Encoding.UTF8, cancellationToken);
        this._logger.LogInformation("Stored manifest in {path}.", nuspecPath);
      }
      {
        var metadataPath = Path.Combine(packageDir, ".metadata");
        File.Delete(metadataPath);
        var doc = new XmlDocument();
        // FIXME: Using a namespace (e.g. urn:nuget:server:package-metadata:1.0) would be better, but also makes working with the
        // FIXME: document more involved.
        var root = doc.CreateElement("package-metadata");
        {
          var packageOwner = doc.CreateElement("owner");
          packageOwner.InnerText = owner;
          root.AppendChild(packageOwner);
        }
        {
          var timestamp = doc.CreateElement("uploaded");
          timestamp.InnerText = DateTimeOffset.UtcNow.ToString("O");
          root.AppendChild(timestamp);
        }
        doc.AppendChild(root);
        doc.Save(metadataPath);
        this._logger.LogInformation("Stored metadata in {path}.", metadataPath);
      }
      // TODO: Construct a URI that points to the corresponding PackageContent.DownloadPackageFile().
      return new CreatedResult("", null);
    }, cancellationToken);
  }

  /// <inheritdoc />
  public async Task<IActionResult> AddSymbolPackageAsync(IFormFile file, string owner, CancellationToken cancellationToken) {
    return await this.WithTempFile<IActionResult>(file, async (tempFile, packageStream) => {
      var info = await this.GetPackageManifestInfo(packageStream, true);
      if (info is null) {
        return new BadRequestResult();
      }
      var (id, version, _) = info.Value;
      var packageDir = Path.Combine(this.PackageFolder, id, version);
      // FIXME: Or should this check for the package file and/or nuspec?
      if (!Directory.Exists(packageDir)) {
        return new NotFoundResult();
      }
      var packagePath = this.StorePackageFile(tempFile, packageDir, id, version, Constants.SymbolsExtension);
      if (packagePath is null) {
        return new ConflictResult();
      }
      {
        var metadataPath = Path.Combine(packageDir, ".metadata");
        if (!File.Exists(metadataPath)) {
          this._logger.LogError("No metadata found for package {id} {version}.", id, version);
          return new ConflictResult();
        }
        XmlDocument doc;
        try {
          doc = new XmlDocument();
          doc.Load(metadataPath);
          if (doc.DocumentElement is null) {
            throw new XmlException("No root element found.");
          }
          if (doc.DocumentElement.LocalName != "package-metadata" || doc.DocumentElement.NamespaceURI != "") {
            throw new XmlException("Incorrect root element.");
          }
        }
        catch (Exception e) {
          this._logger.LogError("Invalid metadata found for package {id} {version}: {e}.", id, version, e);
          return new ConflictResult();
        }
        var root = doc.DocumentElement;
        {
          var storedOwner = root.SelectSingleNode("./owner");
          if (storedOwner is null || storedOwner.InnerText != owner) {
            this._logger.LogError("Adding symbols for package {id} {version} as '{owner}', but package owner is '{storedOwner}'.",
                                  id, version, owner, storedOwner?.InnerText);
            return new UnauthorizedResult();
          }
        }
        {
          var timestamp = doc.CreateElement("symbols-uploaded");
          timestamp.InnerText = DateTimeOffset.UtcNow.ToString("O");
          doc.DocumentElement.AppendChild(timestamp);
        }
        doc.Save(metadataPath);
        this._logger.LogInformation("Updated metadata in {path}.", metadataPath);
      }
      try {
        await using var stream = new FileStream(packagePath, FileMode.Open, FileAccess.Read, FileShare.None);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);
        foreach (var entry in zip.Entries) {
          if (!entry.Name.EndsWith(".pdb")) {
            continue;
          }
          try {
            await using var pdbStream = entry.Open();
            await this._symbolStore.AddSymbolFileAsync(entry.Name.Remove(entry.Name.Length - 4), pdbStream);
          }
          catch (Exception e) {
            this._logger.LogError("Failed to process PDB file ({pdb}) in package: {e}", entry.Name, e);
          }
        }
      }
      catch (Exception e) {
        this._logger.LogError("Failed to process PDB files in package: {e}", e);
      }
      // TODO: Construct a URI that points to the corresponding PackageContent.DownloadPackageFile().
      return new CreatedResult("", null);
    }, cancellationToken);
  }

  /// <inheritdoc />
  public bool DeletePackage(string id, string version) {
    // FIXME: Should this perform any normalization on id and version?
    var path = Path.Combine(this._packageFolder, id, version);
    if (!Directory.Exists(path)) {
      return false;
    }
    // TODO: Maybe determine whether any symbols are stored for this package, and delete those too.
    Directory.Delete(path, true);
    return true;
  }

  /// <inheritdoc />
  public IActionResult GetFile(string id, string version, string file) {
    string contentType;
    string path;
    // Check for the 3 valid file names.
    if (file == $"{id}.{version}{Constants.PackageExtension}") {
      contentType = Constants.PackageContentType;
      path = Path.Combine(this._packageFolder, id, version, file);
    }
    else if (file == $"{id}.{version}{Constants.SymbolsExtension}") {
      contentType = Constants.SymbolsContentType;
      path = Path.Combine(this._packageFolder, id, version, file);
    }
    else if (file == $"{id}{Constants.SpecExtension}") {
      contentType = Constants.SpecContentType;
      // This remaps to the .nuspec file we extract during a push operation.
      path = Path.Combine(this._packageFolder, id, version, Constants.SpecExtension);
    }
    else {
      this._logger.LogWarning("Asked for an unsupported file ({file}) from package {id} {version}.", file, id, version);
      return new NotFoundResult();
    }
    this._logger.LogTrace("File {file} for package {id} {version} maps to {path} ({contentType}).", file, id, version, path,
                          contentType);
    if (!File.Exists(path)) {
      this._logger.LogWarning("Asked for file {file} from package {id} {version}, but it is not available.", file, id, version);
      return new NotFoundResult();
    }
    // FIXME: Any further validation wanted?
    this._logger.LogTrace("Returning file {file} from package {id} {version}.", file, id, version);
    return new FileStreamResult(File.OpenRead(path), contentType) {
      FileDownloadName = file,
      LastModified = File.GetLastWriteTime(path),
    };
  }

  /// <inheritdoc />
  public string? GetPackageOwner(string id, string version) {
    var packageDir = Path.Combine(this.PackageFolder, id, version);
    if (!Directory.Exists(packageDir)) {
      return null;
    }
    var metadataPath = Path.Combine(packageDir, ".metadata");
    if (!File.Exists(metadataPath)) {
      this._logger.LogError("No metadata found for package {id} {version}.", id, version);
      return null;
    }
    XmlDocument doc;
    try {
      doc = new XmlDocument();
      doc.Load(metadataPath);
      if (doc.DocumentElement is null) {
        throw new XmlException("No root element found.");
      }
      if (doc.DocumentElement.LocalName != "package-metadata" || doc.DocumentElement.NamespaceURI != "") {
        throw new XmlException("Incorrect root element.");
      }
    }
    catch (Exception e) {
      this._logger.LogError("Invalid metadata found for package {id} {version}: {e}.", id, version, e);
      return null;
    }
    return doc.DocumentElement.SelectSingleNode("./owner")?.InnerText;
  }

  /// <inheritdoc />
  public IReadOnlyList<string> GetPackageVersions(string id) {
    var packageDir = Path.Combine(this.PackageFolder, id);
    if (!Directory.Exists(packageDir)) {
      return Array.Empty<string>();
    }
    // TODO: Validate version-ness, or just filter out any specific technical directories that might be known to exist.
    return Directory.EnumerateDirectories(packageDir, "*.*", SearchOption.TopDirectoryOnly)
                    .Select(dir => Path.GetFileName(dir) ?? "")
                    .Where(dir => dir.Length > 0)
                    .ToList();
  }

  /// <inheritdoc />
  public bool RelistPackage(string id, string version) {
    // FIXME: Should this perform any normalization on id and version?
    var path = Path.Combine(this._packageFolder, id, version);
    if (!Directory.Exists(path)) {
      return false;
    }
    // FIXME: Is the lock file enough? Or should the "unlisted" status also be in the metadata?
    File.Delete(Path.Combine(path, PackageStore.UnlistedPackageMarker));
    return true;
  }

  /// <inheritdoc />
  public bool UnlistPackage(string id, string version) {
    // FIXME: Should this perform any normalization on id and version?
    var path = Path.Combine(this._packageFolder, id, version);
    if (!Directory.Exists(path)) {
      return false;
    }
    var file = Path.Combine(path, PackageStore.UnlistedPackageMarker);
    if (!File.Exists(file)) {
      File.Create(file).Close();
      File.SetCreationTime(file, DateTime.Now);
    }
    return true;
  }

  #endregion

  #region Internals

  private const string UnlistedPackageMarker = ".unlisted";

  private readonly ILogger<PackageStore> _logger;

  private readonly string _packageFolder;

  private readonly ISymbolStore _symbolStore;

  private readonly string _tempFolder;

  private async Task<ZipArchiveEntry?> GetPackageManifestEntry(ZipArchive zip) {
    var entry = zip.GetEntry("_rels/.rels");
    if (entry is null) {
      this._logger.LogWarning("Invalid package file: No _rels/.rels.");
      return null;
    }
    try {
      await using var stream = entry.Open();
      var doc = new XPathDocument(stream);
      var navigator = doc.CreateNavigator();
      var namespaceManager = new XmlNamespaceManager(navigator.NameTable);
      namespaceManager.AddNamespace("rels", "http://schemas.openxmlformats.org/package/2006/relationships");
      // FIXME: Can the date in this vary like it does in the .nuspec xmlns?
      const string relType = "http://schemas.microsoft.com/packaging/2010/07/manifest";
      const string query = $"/rels:Relationships/rels:Relationship[@Type = '{relType}']/@Target";
      var expression = navigator.Compile(query);
      expression.SetContext(namespaceManager);
      var node = navigator.SelectSingleNode(expression);
      if (node is null) {
        this._logger.LogWarning("Invalid package file: No manifest entry in _rels/.rels.");
        return null;
      }
      var manifestEntryName = node.InnerXml;
      if (manifestEntryName.StartsWith("/")) {
        manifestEntryName = manifestEntryName.Remove(0, 1);
      }
      var manifestEntry = zip.GetEntry(manifestEntryName);
      if (manifestEntry is null) {
        this._logger.LogWarning("Invalid package file: Manifest ({name}) not found.", manifestEntryName);
      }
      return manifestEntry;
    }
    catch (Exception e) {
      this._logger.LogWarning("Invalid package file: Failed to process _rels/.rels: {e}", e);
      return null;
    }
  }

  private async Task<(string id, string version, string nuspec)?> GetPackageManifestInfo(Stream packageStream, bool symbols) {
    try {
      using var zip = new ZipArchive(packageStream, ZipArchiveMode.Read);
      if (zip.GetEntry("[Content_Types].xml") is null) {
        this._logger.LogWarning("Invalid package file: No [Content_Types].xml.");
        return null;
      }
      var entry = await this.GetPackageManifestEntry(zip);
      if (entry is null) {
        return null;
      }
      try {
        await using var stream = entry.Open();
        var doc = new XmlDocument();
        doc.Load(stream);
        var namespaceManager = new XmlNamespaceManager(doc.NameTable);
        {
          var ns = doc.DocumentElement?.NamespaceURI;
          // While a .nuspec file is an XML document that has a namespace, that namespace can differ based on what elements are
          // present. That's fine for schema validation, but annoying for XPath use. So, we check the namespace on the document
          // element, and as long as that looks right, we use it.
          if (ns is null) {
            this._logger.LogWarning("Invalid package file: Empty manifest.");
            return null;
          }
          if (!ns.StartsWith("http://schemas.microsoft.com/packaging/") || !ns.EndsWith("/nuspec.xsd")) {
            this._logger.LogWarning("Invalid package file: Manifest uses unsupported namespace ({ns}).", ns);
            return null;
          }
          namespaceManager.AddNamespace("nu", ns);
        }
        var navigator = doc.CreateNavigator();
        if (navigator is null) {
          this._logger.LogWarning("Invalid package file: Could not navigate the manifest.");
          return null;
        }
        // 3. Get the package ID and version.
        string id;
        {
          const string query = "/nu:package/nu:metadata/nu:id";
          var expression = navigator.Compile(query);
          expression.SetContext(namespaceManager);
          var node = navigator.SelectSingleNode(expression);
          if (node is null) {
            this._logger.LogWarning("Invalid package file: Manifest metadata does not include the package ID.");
            return null;
          }
          id = node.InnerXml;
        }
        string version;
        {
          const string query = "/nu:package/nu:metadata/nu:version";
          var expression = navigator.Compile(query);
          expression.SetContext(namespaceManager);
          var node = navigator.SelectSingleNode(expression);
          if (node is null) {
            this._logger.LogWarning("Invalid package file: Manifest metadata does not include the package version.");
            return null;
          }
          version = node.InnerXml;
        }
        { // Ensure this is a normal package.
          const string query = "/nu:package/nu:metadata/nu:packageTypes/nu:packageType/@name";
          var expression = navigator.Compile(query);
          expression.SetContext(namespaceManager);
          var isSymbolPackage = false;
          foreach (XPathNavigator result in navigator.Select(expression)) {
            var type = result.InnerXml;
            switch (type) {
              case "Dependency":
              case "DotnetTool":
              case "Template":
                if (symbols) {
                  goto default;
                }
                // OK, Supported
                break;
              case "SymbolsPackage":
                if (!symbols) {
                  goto default;
                }
                // OK, Supported
                isSymbolPackage = true;
                break;
              default:
                this._logger.LogWarning("Invalid package file: {id} {v} has unsupported package type ({t}).", id, version, type);
                return null;
            }
          }
          if (symbols && !isSymbolPackage) {
            this._logger.LogWarning("Invalid package file: Not marked as a symbol package.");
            return null;
          }
        }
        this._logger.LogInformation("Received package {id} {v}.", id, version);
        var normalizedId = this.NormalizePackageId(id);
        if (normalizedId is null) {
          this._logger.LogWarning("Package file specifies invalid ID ({id}).", id);
          return null;
        }
        var normalizedVersion = this.NormalizePackageVersion(version);
        if (normalizedVersion is null) {
          this._logger.LogWarning("Package file specifies invalid version ({v}).", version);
          return null;
        }
        return (normalizedId, normalizedVersion, navigator.OuterXml);
      }
      catch (Exception e) {
        this._logger.LogWarning("Invalid package file: Failed to process the manifest: {e}", e);
        return null;
      }
    }
    catch (Exception e) {
      this._logger.LogWarning("Failed to get manifest information from package: {e}", e);
      return null;
    }
  }

  private string? NormalizePackageId(string id) {
    // TODO: More validation, maybe using a regex
    // FIXME: Or should this validation be included in a NormalizePackageId method?
    if (id.IndexOfAny(Path.GetInvalidPathChars()) >= 0 || id.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) {
      return null;
    }
    return id.ToLowerInvariant();
  }

  private string? NormalizePackageVersion(string version) {
    // TODO: Validate OldVersion (n.n.n.n-keyword) or SemanticVersion2 (n.n.n-a.42.c+x.y.z)
    if (version.IndexOfAny(Path.GetInvalidPathChars()) >= 0 || version.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) {
      return null;
    }
    {
      var plus = version.IndexOf('+');
      if (plus > 0) {
        version = version.Remove(plus);
      }
    }
    return version.ToLowerInvariant();
  }

  private string PackageFolder {
    get {
      Directory.CreateDirectory(this._packageFolder);
      return this._packageFolder;
    }
  }

  private string? StorePackageFile(string tempFile, string packageDir, string id, string version, string ext) {
    Directory.CreateDirectory(packageDir);
    var packagePath = Path.Combine(packageDir, $"{id}.{version}{ext}");
    try {
      File.Move(tempFile, packagePath, ext == Constants.SymbolsExtension);
      this._logger.LogInformation("Stored package in {path}.", packagePath);
      return packagePath;
    }
    catch (Exception e) {
      this._logger.LogWarning("Failed to store package in {path}: {e}", packagePath, e);
      return null;
    }
  }

  private string TempFolder {
    get {
      Directory.CreateDirectory(this._tempFolder);
      return this._tempFolder;
    }
  }

  private async Task<T> WithTempFile<T>(IFormFile file, Func<string, Stream, Task<T>> code, CancellationToken cancellationToken) {
    var fileName = Path.Combine(this.TempFolder, Path.GetRandomFileName());
    this._logger.LogInformation("Temporarily storing uploaded file in {fileName}.", fileName);
    try {
      await using var stream = new FileStream(fileName, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
      await file.CopyToAsync(stream, cancellationToken);
      await stream.FlushAsync(cancellationToken);
      stream.Position = 0;
      return await code(fileName, stream);
    }
    finally {
      try {
        File.Delete(fileName);
      }
      catch (Exception e) {
        this._logger.LogWarning("Unable to delete temporary file ({f}): {e}", fileName, e);
      }
    }
  }

  #endregion

}
