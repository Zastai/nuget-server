using Microsoft.AspNetCore.Mvc;

using Zastai.NuGet.Server.Services;

namespace Zastai.NuGet.Server.Controllers.Api;

/// <summary>A package download service.</summary>
[Route(PackageDownload.BasePath)]
public class PackageDownload : ApiController<PackageDownload> {

  private const string BasePath = "packages/content";

  private const string PackageContentType = "application/octet-stream";

  /// <summary>Creates a new package download service controller.</summary>
  /// <param name="logger">A logger for the controller.</param>
  /// <param name="packageStore">The package store to use.</param>
  public PackageDownload(ILogger<PackageDownload> logger, IPackageStore packageStore) : base(logger) {
    this._packageStore = packageStore;
  }

  private readonly IPackageStore _packageStore;

  /// <summary>Retrieve a package.</summary>
  /// <param name="id">The package ID.</param>
  /// <param name="version">The package version.</param>
  /// <param name="file">The name of the package file to retrieve; must be a <c>.nupkg</c> or <c>.snupkg</c> file.</param>
  /// <returns>The requested package if it is available; <c>404 - NOT FOUND</c> otherwise.</returns>
  /// <response code="200">When the requested package is being returned.</response>
  /// <response code="404">When the requested package is not available.</response>
  [HttpGet("{id}/{version}/{file}")]
  [ProducesResponseType(typeof(IEnumerable<byte>), StatusCodes.Status200OK, PackageDownload.PackageContentType)]
  [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
  public IActionResult DownloadPackageFile(string id, string version, string file) {
    // FIXME: Or should this validation be in PackageStore?
    if (!this._packageStore.IsDownloadableFile(file)) {
      this.Logger.LogWarning("Asked for a file from package {id} {version} that is not allowed for download ({file}).", id, version,
                             file);
      return this.NotFound();
    }
    // FIXME: Should this validate that id and version are lower case (and that version is normalized)?
    // FIXME: Should this validate that file == {id}.{version}.[s]nupkg?
    var package = this._packageStore.Open(id, version, file);
    if (package is null) {
      this.Logger.LogWarning("Asked for a file from package {id} {version} that is not available ({file}).", id, version, file);
      return this.NotFound();
    }
    this.Logger.LogInformation("Returning a file from package {id} {version} ({file}).", id, version, file);
    return this.File(package, PackageDownload.PackageContentType, file);
  }

  #region NuGet Service Info

  private const string Description = "Base URL of where NuGet packages are stored.";

  private static readonly string[] Types = {
    "PackageBaseAddress/3.0.0",
  };

  /// <summary>NuGet service information for the package download service.</summary>
  public static readonly NuGetService Service = new(PackageDownload.BasePath, PackageDownload.Types, PackageDownload.Description);

  #endregion

}
