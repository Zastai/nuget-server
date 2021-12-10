using Microsoft.AspNetCore.Mvc;

namespace Zastai.NuGet.Server.Controllers.Api;

/// <summary>A repository signatures service.</summary>
[Route(RepositorySignatures.BasePath)]
public class RepositorySignatures : ApiController<RepositorySignatures> {

  private const string BasePath = "repository-signatures";

  /// <summary>Creates a new repository signatures service controller.</summary>
  /// <param name="logger">A logger for the controller.</param>
  public RepositorySignatures(ILogger<RepositorySignatures> logger) : base(logger) {
  }

  #region NuGet Service Info

  private const string Description = "The endpoint for discovering information about this package source's repository signatures.";

  private static readonly string[] Types = {
    "RepositorySignatures/4.7.0",
    "RepositorySignatures/4.9.0",
    "RepositorySignatures/5.0.0",
  };

  /// <summary>NuGet service information for the repository signatures service.</summary>
  public static readonly NuGetService Service = new(RepositorySignatures.BasePath, RepositorySignatures.Types,
                                                    RepositorySignatures.Description);

  #endregion

}
