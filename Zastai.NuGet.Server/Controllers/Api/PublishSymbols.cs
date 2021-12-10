using Microsoft.AspNetCore.Mvc;

namespace Zastai.NuGet.Server.Controllers.Api;

/// <summary>A symbol package publishing service.</summary>
[Route(PublishSymbols.BasePath)]
public class PublishSymbols : ApiController<PublishSymbols> {

  private const string BasePath = "packages/publish-symbols";

  /// <summary>Creates a new symbol package publishing service controller.</summary>
  /// <param name="logger">A logger for the controller.</param>
  public PublishSymbols(ILogger<PublishSymbols> logger) : base(logger) {
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
