namespace Zastai.NuGet.Server.Services;

/// <summary>Utilities for working with the package store.</summary>
public class PackageStore : IPackageStore {

  /// <summary>Creates a new package store.</summary>
  /// <param name="logger">A logger for the package store.</param>
  /// <param name="host">The host environment.</param>
  public PackageStore(ILogger<PackageStore> logger, IHostEnvironment host) {
    this._logger = logger;
    this._tempFolder = Path.Combine(host.ContentRootPath, "data", "temp", "publish");
    Directory.CreateDirectory(this._tempFolder);
    this._packageFolder = Path.Combine(host.ContentRootPath, "data", "packages");
    Directory.CreateDirectory(this._packageFolder);
  }

  private readonly ILogger<PackageStore> _logger;

  private readonly string _packageFolder;

  private readonly string _tempFolder;

  #region IPackageStore

  /// <inheritdoc />
  public bool IsDownloadableFile(string file) {
    // Only allow packages to be downloaded
    return file.EndsWith(PackageStore.PackageExtension) || file.EndsWith(PackageStore.SymbolPackageExtension);
  }

  /// <inheritdoc />
  public Stream? Open(string id, string version, string file) {
    var path = Path.Combine(this._packageFolder, id.ToLowerInvariant(), version.ToLowerInvariant(), file.ToLowerInvariant());
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

  #endregion

}
