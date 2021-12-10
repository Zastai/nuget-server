namespace Zastai.NuGet.Server;

/// <summary>A NuGet service.</summary>
/// <param name="Path">
/// The path to the service; can be relative (for services implemented by this server) or absolute (for services hosted elsewhere).
/// </param>
/// <param name="Types">The service types (usually in <c>"name/version"</c> format) implemented by this service.</param>
/// <param name="Description">A description of this service.</param>
public sealed record NuGetService(string Path, IEnumerable<string> Types, string Description);
