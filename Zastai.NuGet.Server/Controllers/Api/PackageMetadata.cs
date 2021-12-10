using Microsoft.AspNetCore.Mvc;

namespace Zastai.NuGet.Server.Controllers.Api;

/// <summary>A package metadata service.</summary>
[Route(PackageMetadata.BasePath)]
public class PackageMetadata : ApiController<PackageMetadata> {

  internal const string BasePath = "packages/info";

  /// <summary>Creates a new package metadata service controller.</summary>
  /// <param name="logger">A logger for the controller.</param>
  public PackageMetadata(ILogger<PackageMetadata> logger) : base(logger) {
  }

  #region NuGet Service Info

  private const string Description = "Base URL where NuGet package registration info is stored.";

  private static readonly string[] Types = {
    "RegistrationsBaseUrl",
    "RegistrationsBaseUrl/3.0.0-beta",
    "RegistrationsBaseUrl/3.0.0-rc",
    "RegistrationsBaseUrl/3.4.0", // Return gzip-compressed responses (FIXME: does this require a separate controller?)
    "RegistrationsBaseUrl/3.6.0", // Include SemVer 2.0 packages (FIXME: does this require a separate controller?)
    // NuGet.org also reports a "RegistrationsBaseUrl/Versioned", with a separate 'clientVersion' property set to '4.3.0-alpha'.
  };

  /// <summary>NuGet service information for the package metadata service.</summary>
  public static readonly NuGetService Service = new(PackageMetadata.BasePath, PackageMetadata.Types, PackageMetadata.Description);

  #endregion

}
