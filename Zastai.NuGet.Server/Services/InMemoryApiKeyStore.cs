using System.Collections.Immutable;

using JetBrains.Annotations;

namespace Zastai.NuGet.Server.Services;

/// <summary>An API key store using an in-memory list (loaded from application settings).</summary>
public class InMemoryApiKeyStore : IApiKeyStore {

  /// <summary>Creates a new in-memory API key store.</summary>
  /// <param name="config">The application settings to use.</param>
  /// <param name="logger">The logger to use</param>
  public InMemoryApiKeyStore(IConfiguration config, ILogger<InMemoryApiKeyStore> logger) {
    var configuredKeys = config.GetSection("NuGet:ApiKeys")?.Get<Dictionary<string,ApiKey>?>();
    if (configuredKeys is null) {
      this._contents = ImmutableDictionary<string, ApiKey>.Empty;
    }
    else {
      this._contents = configuredKeys;
    }
    logger.LogInformation("Configured API keys: {count}.", this._contents.Count);
  }

  #region IApiKeyStore

  /// <inheritdoc />
  public Task<IApiKey?> FindKeyAsync(string id)
    => Task.FromResult<IApiKey?>(this._contents.TryGetValue(id, out var key) ? key : null);

  #endregion

  #region Internals

  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  private sealed class ApiKey : IApiKey {

    public bool CanDelete { get; set; }

    public bool CanPublish { get; set; }

    public DateTimeOffset Created { get; set; }

    public DateTimeOffset Expiry { get; set; }

    public string Id { get; set; } = "";

    public string Name { get; set; } = "";

    public string Owner { get; set; } = "";

  }

  private readonly IReadOnlyDictionary<string, ApiKey> _contents;

  #endregion

}
