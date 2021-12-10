using Microsoft.AspNetCore.Mvc;

namespace Zastai.NuGet.Server.Controllers.Api;

/// <summary>An auto-complete search service.</summary>
[Route(Autocomplete.BasePath)]
public class Autocomplete : ApiController<Autocomplete> {

  private const string BasePath = "search/autocomplete";

  /// <summary>Creates a new auto-complete search service controller.</summary>
  /// <param name="logger">A logger for the controller.</param>
  public Autocomplete(ILogger<Autocomplete> logger) : base(logger) {
  }

  #region NuGet Service Info

  private const string Description = "Package search for auto-complete; supports filtering on package type.";

  private static readonly string[] Types = {
    "SearchAutocompleteService",
    "SearchAutocompleteService/3.0.0-beta",
    "SearchAutocompleteService/3.0.0-rc",
    "SearchAutocompleteService/3.0.0",
    "SearchAutocompleteService/3.5.0", // Supports filtering by packageType
  };

  /// <summary>NuGet service information for the auto-complete search service.</summary>
  public static readonly NuGetService Service = new(Autocomplete.BasePath, Autocomplete.Types, Autocomplete.Description);

  #endregion

}
