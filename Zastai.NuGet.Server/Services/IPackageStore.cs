namespace Zastai.NuGet.Server.Services;

/// <summary>A package store.</summary>
public interface IPackageStore {

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

}
