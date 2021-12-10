namespace Zastai.NuGet.Server;

/// <summary>Utilities for working with the package store.</summary>
public static class PackageStore {

  /// <summary>The extension used by package files.</summary>
  public const string PackageExtension = ".nupkg";

  /// <summary>The extension used by symbol package files.</summary>
  public const string SymbolPackageExtension = ".snupkg";

  /// <summary>Attempts to open a NuGet package file.</summary>
  /// <param name="id">The requested package ID.</param>
  /// <param name="version">The requested package version.</param>
  /// <param name="file">The file to open. This must be a <c>.nupkg</c> or <c>.snupkg</c> file.</param>
  /// <returns>A stream for reading the requested package, or <see langword="null"/> if it is not available.</returns>
  public static Stream? Open(string id, string version, string file) {
    string? path = null;
    // TODO: Compose actual path using id and version.
    if (path is null) {
      return null;
    }
    path = Path.Combine(path, file.ToLowerInvariant());
    if (!File.Exists(path)) {
      return null;
    }
    // FIXME: Any further validation wanted?
    return File.OpenRead(path);
  }

}
