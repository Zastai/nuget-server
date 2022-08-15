using Microsoft.AspNetCore.Mvc;

using Zastai.NuGet.Server.Controllers.Documents;
using Zastai.NuGet.Server.Services;

namespace Zastai.NuGet.Server.Controllers.Api;

/// <summary>A package content service.</summary>
[Route(PackageContent.BasePath)]
public class PackageContent : ApiController<PackageContent> {

  private const string BasePath = "packages/content";

  private readonly IPackageStore _packageStore;

  /// <summary>Creates a new package download service controller.</summary>
  /// <param name="logger">A logger for the controller.</param>
  /// <param name="packageStore">The package store to use.</param>
  public PackageContent(ILogger<PackageContent> logger, IPackageStore packageStore) : base(logger) {
    this._packageStore = packageStore;
  }

  /// <summary>Enumerates the versions available for a package.</summary>
  /// <param name="id">The package ID.</param>
  /// <returns>The versions available for a package.</returns>
  /// <response code="200">When the package was found and has available versions.</response>
  /// <response code="404">When the package was not found or does not have any available versions.</response>
  [HttpGet("{id}/index.json")]
  [ProducesResponseType(typeof(PackageVersions), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
  public IActionResult EnumerateVersions(string id) {
    var versions = this._packageStore.GetPackageVersions(id);
    if (versions.Count == 0) {
      return this.NotFound();
    }
    return this.Json(new PackageVersions(versions));
  }

  /// <summary>Retrieve a file related to a package.</summary>
  /// <param name="id">The package ID.</param>
  /// <param name="version">The package version.</param>
  /// <param name="file">
  /// The name of the file to retrieve; must be either <c><paramref name="id"/>.nuspec</c>,
  /// <c><paramref name="id"/>.<paramref name="version"/>.nupkg</c> or
  /// <c><paramref name="id"/>.<paramref name="version"/>.snupkg</c>.
  /// </param>
  /// <returns>The requested file if it is available; <c>404 - NOT FOUND</c> otherwise.</returns>
  /// <response code="200">When the requested file is being returned.</response>
  /// <response code="404">When the requested file is not available.</response>
  [HttpGet("{id}/{version}/{file}")]
  [ProducesResponseType(typeof(IEnumerable<byte>), StatusCodes.Status200OK, Constants.PackageContentType)]
  [ProducesResponseType(typeof(IEnumerable<byte>), StatusCodes.Status200OK, Constants.SpecContentType)]
  [ProducesResponseType(typeof(IEnumerable<byte>), StatusCodes.Status200OK, Constants.SymbolsContentType)]
  [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
  public IActionResult DownloadPackageFile(string id, string version, string file) => this._packageStore.GetFile(id, version, file);

  #region NuGet Service Info

  private const string Description = "Base URL of where NuGet packages are stored.";

  private static readonly string[] Types = {
    "PackageBaseAddress/3.0.0",
  };

  /// <summary>NuGet service information for the package download service.</summary>
  public static readonly NuGetService Service = new(PackageContent.BasePath, PackageContent.Types, PackageContent.Description);

  #endregion

}
