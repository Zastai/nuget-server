using Microsoft.AspNetCore.Mvc;

using Zastai.NuGet.Server.Services;

namespace Zastai.NuGet.Server.Controllers.Api;

/// <summary>A symbol package publishing service.</summary>
[Route(PublishSymbols.BasePath)]
public class PublishSymbols : ApiController<PublishSymbols> {

  private const string BasePath = "packages/publish-symbols";

  /// <summary>Creates a new symbol package publishing service controller.</summary>
  /// <param name="logger">A logger for the controller.</param>
  /// <param name="packageStore">The package store to use.</param>
  public PublishSymbols(ILogger<PublishSymbols> logger, IPackageStore packageStore) : base(logger) {
    this._packageStore = packageStore;
  }

  private readonly IPackageStore _packageStore;

  /// <summary>Publish a symbols package (.snupkg file).</summary>
  /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
  /// <returns>The result of the operation.</returns>
  /// <response code="201">When the package was published successfully.</response>
  /// <response code="400">When the package file is not valid.</response>
  /// <response code="401">When the user is not authorized to publish a symbol package.</response>
  /// <response code="404">When a corresponding non-symbol package is not available.</response>
  /// <response code="409">When the package already exists and cannot be updated.</response>
  /// <response code="413">When the package is too large.</response>
  /// <response code="415">When the request body is not a form containing exactly 1 file.</response>
  [HttpPut]
  [RequireApiKey]
  public async Task<IActionResult> PushAsync(CancellationToken cancellationToken) {
    if (!this.Request.HasFormContentType) {
      return this.StatusCode(StatusCodes.Status415UnsupportedMediaType);
    }
    var form = this.Request.Form;
    if (form.Files.Count != 1) {
      return this.StatusCode(StatusCodes.Status415UnsupportedMediaType, "Exactly one file should be provided.");
    }
    return await this._packageStore.AddSymbolPackageAsync(form.Files[0], cancellationToken);
  }

  #region NuGet Service Info

  private const string Description = "Endpoint for pushing symbols packages.";

  private static readonly string[] Types = {
    "SymbolPackagePublish/4.9.0",
  };

  /// <summary>NuGet service information for the symbol package publishing service.</summary>
  public static readonly NuGetService Service = new(PublishSymbols.BasePath, PublishSymbols.Types, PublishSymbols.Description);

  #endregion

}
