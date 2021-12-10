using Microsoft.AspNetCore.Mvc;

namespace Zastai.NuGet.Server.Controllers.Api;

/// <summary>An abuse reporting service.</summary>
[Route(ReportAbuse.BasePath)]
public sealed class ReportAbuse : ApiController<ReportAbuse> {

  private const string BasePath = "packages/report-abuse";

  private const string ActionPath = "{id}/{version}";

  /// <summary>Creates a new abuse reporting service controller.</summary>
  /// <param name="logger">A logger for the controller.</param>
  public ReportAbuse(ILogger<ReportAbuse> logger) : base(logger) {
  }

  /// <summary>Get the "report abuse" page for a package.</summary>
  /// <param name="id">The requested package id (case does not matter).</param>
  /// <param name="version">The requested package version.</param>
  /// <returns>The details page for the requested package.</returns>
  /// <response code="200">Returns the "report abuse" page for the requested package.</response>
  /// <response code="404">When the requested package does not exist.</response>
  [HttpGet(ReportAbuse.ActionPath)]
  [Produces("text/html")]
  public IActionResult Index(string id, string version) {
    // Pass ID and Version via the view bag for now. It may make more sense to create a model that takes those and get actual
    // package details to be used by the page.
    this.ViewBag.Id = id;
    this.ViewBag.Version = version;
    return this.View();
  }

  #region NuGet Service Info

  private const string Description = "URL template for reporting abuse.";

  private static readonly string[] Types = {
    "ReportAbuseUriTemplate/3.0.0-beta",
    "ReportAbuseUriTemplate/3.0.0-rc",
  };

  /// <summary>NuGet service information for the abuse reporting service.</summary>
  public static readonly NuGetService Service = new(ReportAbuse.BasePath + "/" + ReportAbuse.ActionPath, ReportAbuse.Types,
                                                    ReportAbuse.Description);

  #endregion

}
