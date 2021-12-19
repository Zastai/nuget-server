using Microsoft.AspNetCore.Mvc;

using Zastai.NuGet.Server.Services;

namespace Zastai.NuGet.Server.Controllers.Api;

/// <summary>A package publishing service.</summary>
[Route(PublishPackage.BasePath)]
public class PublishPackage : ApiController<PublishPackage> {

  private const string ActionPath = "{id}/{version}";

  private const string BasePath = "packages/publish";

  /// <summary>Creates a new package publishing service controller.</summary>
  /// <param name="logger">A logger for the controller.</param>
  /// <param name="packageStore">The package store to use.</param>
  public PublishPackage(ILogger<PublishPackage> logger, IPackageStore packageStore) : base(logger) {
    this._packageStore = packageStore;
  }

  private readonly IPackageStore _packageStore;

  /// <summary>Deletes a package from the server, or, if deletion is not allowed, unlists it.</summary>
  /// <param name="id">The package ID.</param>
  /// <param name="version">The package version.</param>
  /// <response code="204">When the package was deleted/unlisted successfully.</response>
  /// <response code="401">When the user is not authorized to delete/unlist a package.</response>
  /// <response code="404">When no matching package was found.</response>
  [HttpDelete(PublishPackage.ActionPath)]
  [RequireApiKey]
  public IActionResult DeleteOrUnlist(string id, string version) {
    // TODO:
    // 1. If delete not allowed: 401
    // 2. Maybe normalize the input.
    // 3. Determine the package path.
    // 4. If it does not exist: 404
    // 5. If it does exist:
    //    - Option A: delete it and 204.
    //    - Option B: unlist it and 204.
    return this.Unauthorized();
  }

  /// <summary>Publish a package (.nupkg file).</summary>
  /// <returns>The result of the operation.</returns>
  /// <response code="201">When the package was published successfully.</response>
  /// <response code="400">When the package file is not valid.</response>
  /// <response code="401">When the user is not authorized to publish a package.</response>
  /// <response code="409">When the package already exists.</response>
  /// <response code="415">When the request body is not a form containing exactly 1 file.</response>
  [HttpPut]
  [RequireApiKey]
  public async Task<IActionResult> Push() {
    if (!this.Request.HasFormContentType) {
      return this.StatusCode(StatusCodes.Status415UnsupportedMediaType);
    }
    var form = this.Request.Form;
    if (form.Files.Count != 1) {
      return this.StatusCode(StatusCodes.Status415UnsupportedMediaType, "Exactly one file should be provided.");
    }
    return await this._packageStore.AddPackageAsync(form.Files[0]);
  }

  /// <summary>Relists a package that was previously unlisted.</summary>
  /// <param name="id">The package ID.</param>
  /// <param name="version">The package version.</param>
  /// <response code="200">When the package was relisted successfully.</response>
  /// <response code="401">When the user is not authorized to delete/unlist a package.</response>
  /// <response code="404">When no matching package was found.</response>
  [HttpPost(PublishPackage.ActionPath)]
  [RequireApiKey]
  public IActionResult Relist(string id, string version) {
    // TODO:
    // 1. If relist not allowed: 401
    // 2. Maybe normalize the input.
    // 3. Determine the package path.
    // 4. If it does not exist: 404
    // 5. If it does exist: relist it and 200.
    return this.Unauthorized();
  }

  #region NuGet Service Info

  private const string Description = "Endpoint for pushing (or deleting/unlisting) packages.";

  private static readonly string[] Types = {
    "PackagePublish/2.0.0",
  };

  /// <summary>NuGet service information for the package publishing service.</summary>
  public static readonly NuGetService Service = new(PublishPackage.BasePath, PublishPackage.Types, PublishPackage.Description);

  #endregion

}
