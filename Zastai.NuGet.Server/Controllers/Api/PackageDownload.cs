using System.Net;

using Microsoft.AspNetCore.Mvc;

namespace Zastai.NuGet.Server.Controllers.Api;

/// <summary>A package download service.</summary>
[Route(PackageDownload.BasePath)]
public class PackageDownload : ApiController<PackageDownload> {

  private const string BasePath = "packages/content";

  private const string PackageContentType = "application/octet-stream";

  /// <summary>Creates a new package download service controller.</summary>
  /// <param name="logger">A logger for the controller.</param>
  public PackageDownload(ILogger<PackageDownload> logger) : base(logger) {
  }

  /// <summary>Retrieve a package.</summary>
  /// <param name="id">The package ID.</param>
  /// <param name="version">The package version.</param>
  /// <param name="file">The name of the package file to retrieve; must be a <c>.nupkg</c> or <c>.snupkg</c> file.</param>
  /// <returns>The requested package if it is available; <c>404 - NOT FOUND</c> otherwise.</returns>
  /// <response code="200">When the requested package is being returned.</response>
  /// <response code="404">When the requested package is not available.</response>
  [HttpGet("{id}/{version}/{file}")]
  [ProducesResponseType(typeof(IEnumerable<byte>), (int) HttpStatusCode.OK, PackageDownload.PackageContentType)]
  [ProducesResponseType(typeof(void), (int) HttpStatusCode.NotFound)]
  public IActionResult DownloadPackageFile(string id, string version, string file) {
    // FIXME: Or should this validation be in PackageStore?
    if (!file.EndsWith(PackageStore.PackageExtension) && !file.EndsWith(PackageStore.SymbolPackageExtension)) {
      this.Logger.LogWarning("Asked for a package file with an invalid extension ({file}) for {id} {version}.", file, id, version);
      return this.NotFound();
    }
    // FIXME: Should this validate that id and version are lower case (and that version is normalized)?
    // FIXME: Should this validate that file == {id}.{version}.[s]nupkg?
    var package = PackageStore.Open(id, version, file);
    if (package is null) {
      this.Logger.LogWarning("Asked for a package file ({file}) for {id} {version}, but it was not available.", file, id, version);
      return this.NotFound();
    }
    this.Logger.LogWarning("Returning package file ({file}) for {id} {version}.", file, id, version);
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
