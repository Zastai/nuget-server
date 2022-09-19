using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;

using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Zastai.NuGet.PackageImport;

internal sealed class Importer : IDisposable {

  public const int DefaultTimeOut = 15;

  // FIXME: Should this set things up to avoid reading/writing the global package folder?
  private readonly SourceCacheContext _cache = new();

  // Microsoft does not believe in a ConcurrentHashSet, so we use a ConcurrentDictionary.
  private readonly ConcurrentDictionary<PackageIdentity, byte> _importedPackages = new();

  private readonly SimpleConsoleLogger _logger;

  private readonly ISettings _settings = new Settings(".");

  private readonly SourceRepository _source;

  private readonly SourceRepository _target;

  public Importer(SimpleConsoleLogger logger, string source, string target) {
    this._logger = logger;
    var repositoryProvider = new CachingSourceProvider(new PackageSourceProvider(this._settings));
    var psp = repositoryProvider.PackageSourceProvider;
    // TODO: Support non-configured feeds
    {
      var ps = psp.GetPackageSourceByName(source);
      if (ps is null || !ps.IsEnabled) {
        logger.LogError($"No source named '{source}' has been configured for NuGet, or it is not enabled.");
        foreach (var knownSource in psp.LoadPackageSources()) {
          logger.LogInformation($"Available source: {knownSource}");
        }
        throw new BadSourceException();
      }
      logger.LogInformation($"Source: {ps}");
      this._source = repositoryProvider.CreateRepository(ps);
    }
    {
      var ps = psp.GetPackageSourceByName(target);
      if (ps is null || !ps.IsEnabled) {
        logger.LogError($"No source named '{target}' has been configured for NuGet, or it is not enabled.");
        foreach (var knownSource in psp.LoadPackageSources()) {
          logger.LogInformation($"Available source: {knownSource}");
        }
        throw new BadSourceException();
      }
      logger.LogInformation($"Target: {ps}");
      this._target = repositoryProvider.CreateRepository(ps);
    }
  }

  public string? ApiKey { get; set; }

  public List<NuGetFramework> DependencyFrameworks { get; } = new();

  public bool DisableBuffering { get; set; }

  public void Dispose() => this._cache.Dispose();

  private async IAsyncEnumerable<PackageIdentity> FindPackagesAsync(SourceRepository repository, string id, VersionRange range,
                                                                    [EnumeratorCancellation] CancellationToken ct = default) {
    var findPackageResource = await repository.GetResourceAsync<FindPackageByIdResource>(ct);
    if (findPackageResource is not null) {
      foreach (var version in await findPackageResource.GetAllVersionsAsync(id, this._cache, this._logger, ct)) {
        if (version is null) {
          continue;
        }
        if (version.IsPrerelease && !this.IncludePrerelease) {
          continue;
        }
        if (range.Satisfies(version)) {
          yield return new PackageIdentity(id, version);
        }
        else {
          await this._logger.LogVerboseAsync($"Skipping version {version} of package {id} - it does not match range {range}.");
        }
      }
    }
    else {
      await this._logger.LogErrorAsync($"No find-package-by-id resource available for {repository}.");
    }
  }

  private async Task<bool> ImportDependencyAsync(PackageDownloadContext ctx, PackageIdentity dependency, NuGetFramework tfm,
                                                 CancellationToken ct = default) {
    if (!this._importedPackages.ContainsKey(dependency)) {
      await this._logger.LogInformationAsync($"Importing dependency {dependency} (target framework: {tfm}).");
      return await this.TryImportAsync(ctx, dependency, ct);
    }
    await this._logger.LogVerboseAsync($"Skipping import for already-imported dependency {dependency} (target " +
                                       $"framework: {tfm}).");
    return true;
  }

  public async Task<bool> ImportPackageListAsync(string file, bool stopOnError = false, CancellationToken ct = default) {
    var errors = 0;
    try {
      var lineNumber = 0;
      foreach (var line in await File.ReadAllLinesAsync(file, Encoding.UTF8, ct)) {
        ++lineNumber;
        if (string.IsNullOrWhiteSpace(line)) {
          continue;
        }
        if (line.TrimStart().StartsWith("# ")) {
          continue;
        }
        var parts = line.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2) {
          await this._logger.LogErrorAsync($"{file}:{lineNumber}: invalid line (need package name and version): '{line}'.");
          goto error;
        }
        if (!await this.ImportSinglePackageAsync(parts[0], parts[1], ct)) {
          goto error;
        }
        continue;
      error:
        ++errors;
        if (stopOnError) {
          break;
        }
      }
    }
    catch (Exception e) {
      await this._logger.LogErrorAsync($"Error reading the package list: {e}");
      ++errors;
    }
    if (errors > 0 && !stopOnError) {
      await this._logger.LogErrorAsync($"Encountered {errors} errors during the import.");
    }
    return errors == 0;
  }

  public async Task<bool> ImportSinglePackageAsync(string id, string versionString, CancellationToken ct = default) {
    VersionRange range;
    if (NuGetVersion.TryParse(versionString, out var version)) {
      // Treat an exact version as an exact-match request (i.e. a range of [version])
      range = new VersionRange(version, true, version, true, null, versionString);
    }
    else if (!VersionRange.TryParse(versionString, true, out range)) {
      await this._logger.LogErrorAsync($"Invalid package version range specified ({versionString}).");
      return false;
    }
    var success = true;
    await this._logger.LogMinimalAsync($"Importing packages matching {id} {range} from {this._source.PackageSource}.");
    var ctx = new PackageDownloadContext(this._cache);
    await foreach (var package in this.FindPackagesAsync(this._source, id, range, ct)) {
      success = await this.TryImportAsync(ctx, package, ct) && success;
    }
    return success;
  }

  public IncludeDependencies IncludeDependencies { get; set; } = IncludeDependencies.None;

  public bool IncludePrerelease { get; set; }

  public bool NoServiceEndpoint { get; set; }

  public bool SkipDuplicates { get; set; }

  public int TimeOut { get; set; } = Importer.DefaultTimeOut;

  private async Task<bool> TryImportAsync(PackageDownloadContext ctx, PackageIdentity id, CancellationToken ct = default) {
    var globalPackages = SettingsUtility.GetGlobalPackagesFolder(this._settings);
    var downloader = await this._source.GetResourceAsync<DownloadResource>(ct);
    if (downloader is null) {
      await this._logger.LogErrorAsync("Could not determine how to download packages from the specified source feed.");
      return false;
    }
    using var downloadResult = await downloader.GetDownloadResourceResultAsync(id, ctx, globalPackages, this._logger, ct);
    if (downloadResult is null) {
      await this._logger.LogErrorAsync($"An attempt to download package {id} produced no result.");
      return false;
    }
    switch (downloadResult.Status) {
      case DownloadResourceResultStatus.Available:
        // OK
        break;
      case DownloadResourceResultStatus.AvailableWithoutStream:
        await this._logger.LogErrorAsync($"Package {id} was found in source '{this._source}', but its data was not available.");
        return false;
      case DownloadResourceResultStatus.Cancelled:
        await this._logger.LogErrorAsync($"The download of package {id} was canceled.");
        return false;
      case DownloadResourceResultStatus.NotFound:
        await this._logger.LogErrorAsync($"Package {id} was not found in source '{this._source}'.");
        return false;
      default:
        await this._logger.LogErrorAsync($"The download of package {id} returned an unhandled status ({downloadResult.Status}).");
        return false;
    }
    var packageSize = downloadResult.PackageStream.Length;
    // FIXME: This should (perhaps based on a flag) acquire the symbols package too - but there does not seem to be an endpoint
    //        for retrieving the snupkg, only for pushing it.
    var pusher = await this._target.GetResourceAsync<PackageUpdateResource>(ct);
    if (pusher is null) {
      await this._logger.LogErrorAsync("Could not determine how to push packages to the specified target feed.");
      return false;
    }
    {
      // We have no clear way of downloading symbol packages, so don't push them either.
      const string? symbolSource = null;
      const SymbolPackageUpdateResourceV3? symbolPackageUpdateResource = null;
      var packageTempFile = Path.GetTempFileName();
      try {
        await using (var file = File.OpenWrite(packageTempFile)) {
          await downloadResult.PackageStream.CopyToAsync(file, ct);
        }
        var packagePaths = new List<string> { packageTempFile };
        var ps = this._target.PackageSource;
        await this._logger.LogMinimalAsync($"Pushing package {id} ({packageSize} byte(s)) to {ps}...");
        try {
          await pusher.Push(packagePaths, symbolSource, this.TimeOut, this.DisableBuffering, _ => this.ApiKey, null,
                            this.NoServiceEndpoint, this.SkipDuplicates, symbolPackageUpdateResource, this._logger);
        }
        catch (TaskCanceledException) {
          await this._logger.LogErrorAsync($"Push to {ps} timed out for package {id}.");
          return false;
        }
        catch (Exception e) {
          await this._logger.LogErrorAsync($"Push to {ps} for package {id} failed: {e}");
          return false;
        }
      }
      finally {
        File.Delete(packageTempFile);
      }
    }
    this._importedPackages.TryAdd(id, 42);
    if (this.IncludeDependencies == IncludeDependencies.None) {
      return true;
    }
    var success = true;
    foreach (var pdg in await downloadResult.PackageReader.GetPackageDependenciesAsync(ct)) {
      var tfm = pdg.TargetFramework;
      if (this.DependencyFrameworks.Count != 0 && !this.DependencyFrameworks.Contains(tfm)) {
        await this._logger.LogVerboseAsync($"Skipping dependency group for target framework '{tfm}'.");
        continue;
      }
      foreach (var pd in pdg.Packages) {
        PackageIdentity? selectedDependency = null;
        await foreach (var dependency in this.FindPackagesAsync(this._source, pd.Id, pd.VersionRange, ct)) {
          if (this.IncludeDependencies == IncludeDependencies.BestMatch) {
            if (pd.VersionRange.IsBetter(selectedDependency?.Version, dependency.Version)) {
              selectedDependency = dependency;
            }
            continue;
          }
          success = await this.ImportDependencyAsync(ctx, dependency, tfm, ct) && success;
        }
        if (selectedDependency is not null) {
          success = await this.ImportDependencyAsync(ctx, selectedDependency, tfm, ct) && success;
        }
      }
    }
    return success;
  }

}
