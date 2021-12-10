using Microsoft.AspNetCore.Mvc;

namespace Zastai.NuGet.Server.Controllers.Api;

/// <summary>A catalog service.</summary>
[Route(Catalog.BasePath)]
public class Catalog : ApiController<Catalog> {

  private const string BasePath = "catalog";

  /// <summary>Creates a new catalog service controller.</summary>
  /// <param name="logger">A logger for the controller.</param>
  public Catalog(ILogger<Catalog> logger) : base(logger) {
  }

  #region NuGet Service Info

  private const string Description = "Catalog of all package operations.";

  private static readonly string[] Types = {
    "Catalog/3.0.0",
  };

  /// <summary>NuGet service information for the catalog service.</summary>
  public static readonly NuGetService Service = new(Catalog.BasePath, Catalog.Types, Catalog.Description);

  #endregion

}
