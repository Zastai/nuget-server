using Microsoft.AspNetCore.Mvc;

namespace Zastai.NuGet.Server.Services;

/// <summary>A package store.</summary>
public interface IPackageStore {

  /// <summary>Adds a package to the store.</summary>
  /// <param name="file">The package file.</param>
  /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
  /// <returns>The result of the operation.</returns>
  public Task<IActionResult> AddPackageAsync(IFormFile file, CancellationToken cancellationToken = new());

  /// <summary>Adds a symbol package to the store.</summary>
  /// <param name="file">The symbol package file.</param>
  /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
  /// <returns>The result of the operation.</returns>
  public Task<IActionResult> AddSymbolPackageAsync(IFormFile file, CancellationToken cancellationToken = new());

  /// <summary>Deletes the specified package.</summary>
  /// <param name="id">The package ID.</param>
  /// <param name="version">The package version.</param>
  /// <returns><see langword="false"/> if the package did not exist; <see langword="true"/> otherwise.</returns>
  public bool DeletePackage(string id, string version);

  /// <summary>Determines whether or not a give file is allowed to be downloaded.</summary>
  /// <param name="file">The file for which download is requested.</param>
  /// <returns>
  /// <see langword="true"/> when <paramref name="file"/> is allowed for download; <see langword="false"/> otherwise.
  /// </returns>
  public bool IsDownloadableFile(string file);

  /// <summary>Attempts to open a NuGet package file.</summary>
  /// <param name="id">The requested package ID.</param>
  /// <param name="version">The requested package version.</param>
  /// <param name="file">The file to open. This must be a <c>.nupkg</c> or <c>.snupkg</c> file.</param>
  /// <returns>A stream for reading the requested package, or <see langword="null"/> if it is not available.</returns>
  public Stream? Open(string id, string version, string file);

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
