using Microsoft.AspNetCore.Mvc;

namespace Zastai.NuGet.Server.Controllers.Api;

/// <summary>A package search service.</summary>
[Route(Search.BasePath)]
public class Search : ApiController<Search> {

  private const string BasePath = "search/query";

  /// <summary>Creates a new search service controller.</summary>
  /// <param name="logger">A logger for the controller.</param>
  public Search(ILogger<Search> logger) : base(logger) {
  }

  #region NuGet Service Info

  private const string Description = "Package search; supports filtering on package type.";

  private static readonly string[] Types = {
    "SearchQueryService",
    "SearchQueryService/3.0.0-beta",
    "SearchQueryService/3.0.0-rc",
    "SearchQueryService/3.5.0", // Supports the packageType query parameter
  };

  /// <summary>NuGet service information for the search service.</summary>
  public static readonly NuGetService Service = new(Search.BasePath, Search.Types, Search.Description);

  #endregion

}
