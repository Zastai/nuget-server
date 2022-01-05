namespace Zastai.NuGet.Server.Services;

/// <summary>A store containing user information.</summary>
public interface IUserStore {

  /// <summary>Finds a particular user in the store.</summary>
  /// <param name="id">The ID of the user to find.</param>
  /// <returns>The requested user, or <see langword="null"/> if none was found.</returns>
  public Task<IUser?> FindUserAsync(string id);

}
