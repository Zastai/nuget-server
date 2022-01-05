using System.Collections.Immutable;

using JetBrains.Annotations;

namespace Zastai.NuGet.Server.Services;

/// <summary>A user store using an in-memory list (loaded from application settings).</summary>
public class InMemoryUserStore : IUserStore {

  /// <summary>Creates a new API store using the application settings.</summary>
  /// <param name="config">The application settings to use.</param>
  /// <param name="logger">The logger to use</param>
  public InMemoryUserStore(IConfiguration config, ILogger<InMemoryUserStore> logger) {
    var configuredUsers = config.GetSection("NuGet:Users")?.Get<Dictionary<string,User>?>();
    if (configuredUsers is null) {
      this._contents = ImmutableDictionary<string, User>.Empty;
    }
    else {
      this._contents = configuredUsers;
    }
    logger.LogInformation("Configured users: {count}.", this._contents.Count);
  }

  #region IUserStore

  /// <inheritdoc />
  public Task<IUser?> FindUserAsync(string id)
    => Task.FromResult<IUser?>(this._contents.TryGetValue(id, out var user) ? user : null);

  #endregion

  #region Internals

  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  private sealed class User : IUser {

    public bool CanDelete { get; set; }

    public bool CanPublish { get; set;  }

    public DateTimeOffset Created { get; set; }

    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

  }

  private readonly IReadOnlyDictionary<string, User> _contents;

  #endregion

}
