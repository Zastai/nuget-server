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
  public PackageStore(ILogger<PackageStore> logger, IHostEnvironment host) {
    this._logger = logger;
    this._tempFolder = Path.Combine(host.ContentRootPath, "data", "temp", "publish");
    this._packageFolder = Path.Combine(host.ContentRootPath, "data", "packages");
  }

  private readonly ILogger<PackageStore> _logger;

  #region IPackageStore

  /// <inheritdoc />
  public async Task<IActionResult> AddPackageAsync(IFormFile file) {
    return await this.WithTempFile<IActionResult>(file, async (tempFile, packageStream) => {
      var info = await this.GetPackageManifestInfo(packageStream, false);
      if (info is null) {
        return new BadRequestResult();
      }
      var (id, version, nuspec) = info.Value;
      var packageDir = Path.Combine(this.PackageFolder, id, version);
      // FIXME: Or should this check for the package file and/or nuspec?
      if (Directory.Exists(packageDir)) {
        return new ConflictResult();
      }
      if (!this.StorePackageFile(tempFile, packageDir, id, version, PackageStore.PackageExtension)) {
        return new ConflictResult();
      }
      { // Store the manifest in a separate file too
        var nuspecPath = Path.Combine(packageDir, ".nuspec");
        File.Delete(nuspecPath);
        await File.WriteAllTextAsync(nuspecPath, nuspec, Encoding.UTF8);
        this._logger.LogInformation("Stored manifest in {path}.", nuspecPath);
      }
      // FIXME: Should this also generate the JSON metadata or could/should that be left to a separate indexing services?
      // TODO: Construct a URI that points to the corresponding PackageDownload.DownloadPackageFile().
      return new CreatedResult("", null);
    });
  }

  /// <inheritdoc />
  public async Task<IActionResult> AddSymbolPackageAsync(IFormFile file) {
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
      if (!this.StorePackageFile(tempFile, packageDir, id, version, PackageStore.SymbolPackageExtension)) {
        return new ConflictResult();
      }
      // TODO: Scan the package for .pdb files and add them to the symbol store.
      // TODO: Construct a URI that points to the corresponding PackageDownload.DownloadPackageFile().
      return new CreatedResult("", null);
    });
  }

  /// <inheritdoc />
  public bool IsDownloadableFile(string file) {
    // Only allow packages to be downloaded
    return file.EndsWith(PackageStore.PackageExtension) || file.EndsWith(PackageStore.SymbolPackageExtension);
  }

  /// <inheritdoc />
  public Stream? Open(string id, string version, string file) {
    // FIXME: Should this perform any normalization on id and version?
    var path = Path.Combine(this._packageFolder, id, version, file);
    if (!File.Exists(path)) {
      return null;
    }
    // FIXME: Any further validation wanted?
    return File.OpenRead(path);
  }

  #endregion

  #region Internals

  /// <summary>The extension used by package files.</summary>
  private const string PackageExtension = ".nupkg";

  /// <summary>The extension used by symbol package files.</summary>
  private const string SymbolPackageExtension = ".snupkg";

  private readonly string _packageFolder;

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

  private bool StorePackageFile(string tempFile, string packageDir, string id, string version, string ext) {
    Directory.CreateDirectory(packageDir);
    var packagePath = Path.Combine(packageDir, $"{id}.{version}{ext}");
    try {
      File.Move(tempFile, packagePath, ext == PackageStore.SymbolPackageExtension);
      this._logger.LogInformation("Stored package in {path}.", packagePath);
      return true;
    }
    catch (Exception e) {
      this._logger.LogWarning("Failed to store package in {path}: {e}", packagePath, e);
      return false;
    }
  }

  private string TempFolder {
    get {
      Directory.CreateDirectory(this._tempFolder);
      return this._tempFolder;
    }
  }

  private async Task<T> WithTempFile<T>(IFormFile file, Func<string, Stream, Task<T>> code) {
    var fileName = Path.Combine(this.TempFolder, Path.GetRandomFileName());
    this._logger.LogInformation("Temporarily storing uploaded file in {fileName}.", fileName);
    try {
      await using var stream = new FileStream(fileName, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
      await file.CopyToAsync(stream);
      await stream.FlushAsync();
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
