namespace Zastai.NuGet.Server.Services;

/// <summary>Settings based on standard ASP.NET configuration.</summary>
public class Settings : ISettings {

  /// <summary>Creates a new set of settings, based on standard ASP.NET configuration.</summary>
  /// <param name="configuration">The ASP.NET configuration.</param>
  public Settings(IConfiguration configuration) {
    this._configuration = configuration;
  }

  private readonly IConfiguration _configuration;

  /// <inheritdoc />
  public bool IsDeleteAllowed => this._configuration.GetValue<bool>("NuGet:AllowDelete");

  /// <inheritdoc />
  public bool IsRelistAllowed => this._configuration.GetValue<bool>("NuGet:AllowRelist");

  /// <inheritdoc />
  public bool IsUnlistAllowed => this._configuration.GetValue<bool>("NuGet:AllowUnlist");

}
