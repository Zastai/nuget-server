using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;

using Zastai.NuGet.Server.Auth;
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
  /// <param name="settings">The applicable settings.</param>
  public PublishPackage(ILogger<PublishPackage> logger, IPackageStore packageStore, ISettings settings) : base(logger) {
    this._packageStore = packageStore;
    this._settings = settings;
  }

  private readonly IPackageStore _packageStore;

  private readonly ISettings _settings;

  /// <summary>Deletes a package from the server, or, if deletion is not allowed, unlists it.</summary>
  /// <param name="id">The package ID.</param>
  /// <param name="version">The package version.</param>
  /// <response code="204">When the package was deleted/unlisted successfully.</response>
  /// <response code="401">When the user is not authorized to delete/unlist a package.</response>
  /// <response code="404">When no matching package was found.</response>
  [HttpDelete(PublishPackage.ActionPath)]
  [RequireApiKey(Roles = NuGetRoles.CanDeletePackages)]
  public IActionResult DeleteOrUnlist(string id, string version) {
    if (!this.UserIsPackageOwner(id, version, out var result)) {
      return result;
    }
    if (this._settings.IsDeleteAllowed) {
      return this._packageStore.DeletePackage(id, version) ? this.NoContent() : this.NotFound();
    }
    if (this._settings.IsUnlistAllowed) {
      return this._packageStore.UnlistPackage(id, version) ? this.NoContent() : this.NotFound();
    }
    this.Logger.LogError("Delete or unlist requested, but neither is allowed by configuration.");
    return this.Unauthorized();
  }

  /// <summary>Publish a package (.nupkg file).</summary>
  /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
  /// <returns>The result of the operation.</returns>
  /// <response code="201">When the package was published successfully.</response>
  /// <response code="400">When the package file is not valid.</response>
  /// <response code="401">When the user is not authorized to publish a package.</response>
  /// <response code="409">When the package already exists.</response>
  /// <response code="415">When the request body is not a form containing exactly 1 file.</response>
  [HttpPut]
  [RequireApiKey(Roles = NuGetRoles.CanPublishPackages)]
  public async Task<IActionResult> PushAsync(CancellationToken cancellationToken) {
    if (!this.Request.HasFormContentType) {
      return this.StatusCode(StatusCodes.Status415UnsupportedMediaType);
    }
    var form = this.Request.Form;
    if (form.Files.Count != 1) {
      return this.StatusCode(StatusCodes.Status415UnsupportedMediaType, "Exactly one file should be provided.");
    }
    var user = this.User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Actor)?.Value;
    if (user is not null) {
      return await this._packageStore.AddPackageAsync(form.Files[0], user, cancellationToken);
    }
    this.Logger.LogError("Could not determine the ID of the user pushing the package.");
    return this.Unauthorized();
  }

  /// <summary>Relists a package that was previously unlisted.</summary>
  /// <param name="id">The package ID.</param>
  /// <param name="version">The package version.</param>
  /// <response code="200">When the package was relisted successfully.</response>
  /// <response code="401">When the user is not authorized to delete/unlist a package.</response>
  /// <response code="404">When no matching package was found.</response>
  [HttpPost(PublishPackage.ActionPath)]
  [RequireApiKey(Roles = NuGetRoles.CanDeletePackages)]
  public IActionResult Relist(string id, string version) {
    if (!this.UserIsPackageOwner(id, version, out var result)) {
      return result;
    }
    if (this._settings.IsRelistAllowed) {
      return this._packageStore.RelistPackage(id, version) ? this.Ok() : this.NotFound();
    }
    this.Logger.LogError("Relist requested, but not allowed by configuration.");
    return this.Unauthorized();
  }

  private bool UserIsPackageOwner(string id, string version, [NotNullWhen(false)] out IActionResult? result) {
    var user = this.User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Actor)?.Value;
    if (user is not null) {
      var owner = this._packageStore.GetPackageOwner(id, version);
      if (owner is null) {
        result = this.NotFound();
        return false;
      }
      if (owner == user) {
        result = null;
        return true;
      }
      this.Logger.LogError("The user making the request ({u}) is not the package owner ({o}).", user, owner);
    }
    else {
      this.Logger.LogError("Could not determine the ID of the user requesting the package unlist.");
    }
    result = this.Unauthorized();
    return false;
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
