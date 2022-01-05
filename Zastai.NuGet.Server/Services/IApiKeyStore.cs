namespace Zastai.NuGet.Server.Services;

/// <summary>
/// A store containing API keys.<br/>
/// These will link to a user ID (with further information to be taken from an <see cref="IUserStore"/>) as well as access rights
/// (one per operation secured via API key, such as "push package" and "delete/unlist package").
/// </summary>
public interface IApiKeyStore {

  /// <summary>Finds a particular API key in the store.</summary>
  /// <param name="id">The ID of the key to find.</param>
  /// <returns>The requested API key, or <see langword="null"/> if none was found.</returns>
  public Task<IApiKey?> FindKeyAsync(string id);

}
