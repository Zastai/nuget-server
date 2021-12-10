using Microsoft.AspNetCore.Mvc;

namespace Zastai.NuGet.Server.Controllers.Api;

/// <summary>A package details service.</summary>
[Route(PackageDetails.BasePath)]
public sealed class PackageDetails : ApiController<PackageDetails> {

  private const string BasePath = "packages/details";

  private const string ActionPath = "{id}/{version}";

  /// <summary>Creates a new package details service controller.</summary>
  /// <param name="logger">A logger for the controller.</param>
  public PackageDetails(ILogger<PackageDetails> logger) : base(logger) {
  }

  /// <summary>Get the details page for a package.</summary>
  /// <param name="id">The requested package id (case does not matter).</param>
  /// <param name="version">The requested package version.</param>
  /// <returns>The details page for the requested package.</returns>
  /// <response code="200">Returns the details page for the requested package.</response>
  /// <response code="404">When the requested package does not exist.</response>
  [HttpGet(PackageDetails.ActionPath)]
  [Produces("text/html")]
  public IActionResult Index(string id, string version) {
    // Pass ID and Version via the view bag for now. It may make more sense to create a model that takes those and get actual
    // package details to be used by the page.
    this.ViewBag.Id = id;
    this.ViewBag.Version = version;
    return this.View();
  }

  #region NuGet Service Info

  private const string Description = "URL template for package details.";

  private static readonly string[] Types = {
    "PackageDetailsUriTemplate/5.1.0",
  };

  /// <summary>NuGet service information for the package details service.</summary>
  public static readonly NuGetService Service = new(PackageDetails.BasePath + "/" + PackageDetails.ActionPath, PackageDetails.Types,
                                                    PackageDetails.Description);

  #endregion

}
