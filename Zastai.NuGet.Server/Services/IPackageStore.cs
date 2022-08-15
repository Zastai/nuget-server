using Microsoft.AspNetCore.Mvc;

namespace Zastai.NuGet.Server.Services;

/// <summary>A package store.</summary>
public interface IPackageStore {

  /// <summary>Adds a package to the store.</summary>
  /// <param name="file">The package file.</param>
  /// <param name="owner">The ID of the user adding the package.</param>
  /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
  /// <returns>The result of the operation.</returns>
  public Task<IActionResult> AddPackageAsync(IFormFile file, string owner, CancellationToken cancellationToken = new());

  /// <summary>Adds a symbol package to the store.</summary>
  /// <param name="file">The symbol package file.</param>
  /// <param name="owner">The ID of the user adding the package.</param>
  /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
  /// <returns>The result of the operation.</returns>
  public Task<IActionResult> AddSymbolPackageAsync(IFormFile file, string owner, CancellationToken cancellationToken = new());

  /// <summary>Deletes the specified package.</summary>
  /// <param name="id">The package ID.</param>
  /// <param name="version">The package version.</param>
  /// <returns><see langword="false"/> if the package did not exist; <see langword="true"/> otherwise.</returns>
  public bool DeletePackage(string id, string version);

  /// <summary>Attempts to return a file related to a NuGet package.</summary>
  /// <param name="id">The requested package ID.</param>
  /// <param name="version">The requested package version.</param>
  /// <param name="file">
  /// The file to open. This must be a suitable file name related to <paramref name="id"/> and <paramref name="version"/>.
  /// </param>
  /// <returns>The requested file as a <see cref="FileResult"/>, if available; otherwise, a <see cref="NotFoundResult"/>.</returns>
  public IActionResult GetFile(string id, string version, string file);

  /// <summary>Retrieves the owner of the package.</summary>
  /// <param name="id">The package ID.</param>
  /// <param name="version">The package version.</param>
  /// <returns>The ID of the user who added the package; <see langword="null"/> if the package did not exist.</returns>
  public string? GetPackageOwner(string id, string version);

  /// <summary>Retrieves the available versions for a package.</summary>
  /// <param name="id">The package ID.</param>
  /// <returns>The versions available for the package.</returns>
  public IReadOnlyList<string> GetPackageVersions(string id);

  /// <summary>Relists the specified package (if it was previously unlisted).</summary>
  /// <param name="id">The package ID.</param>
  /// <param name="version">The package version.</param>
  /// <returns><see langword="false"/> if the package did not exist; <see langword="true"/> otherwise.</returns>
  public bool RelistPackage(string id, string version);

  /// <summary>Unlists the specified package.</summary>
  /// <param name="id">The package ID.</param>
  /// <param name="version">The package version.</param>
  /// <returns><see langword="false"/> if the package did not exist; <see langword="true"/> otherwise.</returns>
  public bool UnlistPackage(string id, string version);

}
