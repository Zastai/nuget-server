using Microsoft.AspNetCore.Mvc;

namespace Zastai.NuGet.Server.Controllers.Api;

/// <summary>A package publishing service.</summary>
[Route(PublishPackage.BasePath)]
public class PublishPackage : ApiController<PublishPackage> {

  private const string BasePath = "packages/publish";

  /// <summary>Creates a new package publishing service controller.</summary>
  /// <param name="logger">A logger for the controller.</param>
  public PublishPackage(ILogger<PublishPackage> logger) : base(logger) {
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
