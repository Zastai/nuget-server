using System.Globalization;

using NuGet.Common;
using NuGet.Frameworks;

namespace Zastai.NuGet.PackageImport;

public static class Program {

  private static readonly string[] UsageText = {
    "Usage: PackageImport FromSource ToSource Package VersionRange [OPTIONS]",
    "   or: PackageImport FromSource ToSource -List PackageListFile [OPTIONS]",
    "",
    "Downloads the specified package (or set of packages read from a file) from a given NuGet source and pushes them to a ",
    "different NuGet feed. Its intended use is to populate a local folder or server with packages from nuget.org.",
    "",
    "Currently, both sources must be source names defined by a NuGet.config file.",
    "",
    "When using a file, blank lines and lines starting with '# ' are ignored. All other lines are expected to contain a package ID",
    "followed by a single NuGet version range, separated by whitespace.",
    "",
    "Options:",
    " -ApiKey <key>                                    Use the specified API key when pushing packages.",
    " -DisableBuffering                                Disables buffering when pushing packages.",
    " -DependencyFrameworks <target-framework-list>    Only consider dependencies for a specific set of target frameworks.",
    " -IncludeDependencies <mode>                      With mode 'BestMatch', only the best matching version will be imported.",
    "                                                  With mode 'All', all matching versions will be imported; be aware that most",
    "                                                  NuGet dependencies are specified open-ended, so this can result in a large",
    "                                                  number of imports. With mode 'None' (the default), no dependencies are",
    "                                                  imported automatically.",
    " -IncludePrerelease                               When specified, pre-release package versions are also considered when",
    "                                                  resolving version ranges.",
    $" -LogLevel <level>                                Set the log level; defaults to '{SimpleConsoleLogger.DefaultMinimumLevel}'.",
    " -NoServiceEndpoint                               Does not append 'api/v2/packages' to the source URL.",
    " -SkipDuplicates                                  When pushing, skip a package version that already exists in the target feed.",
    $" -TimeOut <seconds>                               Sets the timeout for pushing to a server, in seconds. Defaults to {Importer.DefaultTimeOut}.",
    "",
    "Options can also be abbreviated to their initials, e.g. -nse for -NoServiceEndpoint.",
    "Option values can also be specified after a colon, e.g. -TimeOut:42.",
  };

  private static async Task<int> UsageAsync() {
    foreach (var line in Program.UsageText) {
      await Console.Out.WriteLineAsync(line);
    }
    return 1;
  }

  public static async Task<int> Main(string[] args) {
    var cancellation = new CancellationTokenSource();
    var logger = new SimpleConsoleLogger();
    if (args.Length < 4) {
      return await Program.UsageAsync();
    }
    var fromSource = args[0];
    var targetSource = args[1];
    string? packageList = null;
    string? packageName = null;
    string? packageVersion = null;
    if (args[2].ToLowerInvariant() == "-list") {
      packageList = args[3];
    }
    else {
      packageName = args[2];
      packageVersion = args[3];
    }
    string? apiKey = null;
    var disableBuffering = false;
    var includeDependencies = IncludeDependencies.None;
    var includePrerelease = false;
    var noServiceEndpoint = false;
    var skipDuplicates = false;
    int? timeOut = null;
    var dependencyFrameworks = new List<NuGetFramework>();
    for (var i = 4; i < args.Length; ++i) {
      var option = args[i];
      string? argument = null;
      if (option.StartsWith('-') && option.Contains(':')) {
        var parts = option.Split(':', 2);
        option = parts[0];
        argument = parts[1];
      }
      switch (option.ToLowerInvariant()) {
        case "-ak":
        case "-apikey": {
          if (argument is null && i <= args.Length) {
            argument = args[++i];
          }
          if (argument is null) {
            await logger.LogErrorAsync("Missing argument for -ApiKey.");
            goto default;
          }
          apiKey = argument;
          break;
        }
        case "-db":
        case "-disablebuffering":
          if (argument is not null) {
            await logger.LogErrorAsync("Argument specified for -DisableBuffering.");
            goto default;
          }
          disableBuffering = true;
          break;
        case "-df":
        case "-dependencyframeworks": {
          if (argument is null && i <= args.Length) {
            argument = args[++i];
          }
          if (argument is null) {
            await logger.LogErrorAsync("Missing argument for -DependencyFrameworks.");
            goto default;
          }
          var nameProvider = new DefaultFrameworkNameProvider();
          var parts = argument.Split(',');
          foreach (var part in parts) {
            try {
              dependencyFrameworks.Add(NuGetFramework.ParseFrameworkName(part.Trim(), nameProvider));
            }
            catch (Exception e) {
              await logger.LogErrorAsync($"Invalid argument for -DependencyFrameworks ('{part}'): {e.Message}.");
            }
          }
          break;
        }
        case "-id":
        case "-includedependencies": {
          if (argument is null && i <= args.Length) {
            argument = args[++i];
          }
          if (argument is null) {
            await logger.LogErrorAsync("Missing argument for -IncludeDependencies.");
            goto default;
          }
          if (Enum.TryParse<IncludeDependencies>(argument, true, out var value)) {
            includeDependencies = value;
          }
          else {
            await logger.LogErrorAsync($"Invalid argument for -IncludeDependencies ('{argument}').");
            goto default;
          }
          break;
        }
        case "-ip":
        case "-includeprerelease":
          if (argument is not null) {
            await logger.LogErrorAsync("Argument specified for -IncludePrerelease.");
            goto default;
          }
          includePrerelease = true;
          break;
        case "-ll":
        case "-loglevel": {
          if (argument is null && i <= args.Length) {
            argument = args[++i];
          }
          if (argument is null) {
            await logger.LogErrorAsync("Missing argument for -LogLevel.");
            goto default;
          }
          if (Enum.TryParse<LogLevel>(argument, true, out var value)) {
            logger.MinimumLevel = value;
          }
          else {
            await logger.LogErrorAsync($"Invalid argument for -LogLevel ('{argument}'); default values are " +
                                       $"'{string.Join("', '", Enum.GetNames<LogLevel>())}'.");
            goto default;
          }
          break;
        }
        case "-nse":
        case "-noserviceendpoint":
          if (argument is not null) {
            await logger.LogErrorAsync("Argument specified for -NoServiceEndpoint.");
            goto default;
          }
          noServiceEndpoint = true;
          break;
        case "-sd":
        case "-skipduplicates":
          if (argument is not null) {
            await logger.LogErrorAsync("Argument specified for -SkipDuplicates.");
            goto default;
          }
          skipDuplicates = true;
          break;
        case "-to":
        case "-timeout": {
          if (argument is null && i <= args.Length) {
            argument = args[++i];
          }
          if (argument is null) {
            await logger.LogErrorAsync("Missing argument for -TimeOut.");
            goto default;
          }
          if (int.TryParse(argument.Trim(), NumberStyles.None, CultureInfo.CurrentCulture, out var value)) {
            timeOut = value;
          }
          else {
            await logger.LogErrorAsync($"Invalid argument for -TimeOut ('{argument}').");
            goto default;
          }
          break;
        }
        default:
          return await Program.UsageAsync();
      }
    }
    Importer importer;
    try {
      importer = new Importer(logger, fromSource, targetSource) {
        ApiKey = apiKey,
        DisableBuffering = disableBuffering,
        IncludeDependencies = includeDependencies,
        IncludePrerelease = includePrerelease,
        NoServiceEndpoint = noServiceEndpoint,
        SkipDuplicates = skipDuplicates,
      };
      importer.DependencyFrameworks.AddRange(dependencyFrameworks);
      if (timeOut.HasValue) {
        importer.TimeOut = timeOut.Value;
      }
    }
    catch (BadSourceException) {
      return 2;
    }
    if (packageList is not null) {
      if (!File.Exists(packageList)) {
        await logger.LogErrorAsync($"The specified package list ({packageList}) does not exist.");
        return 3;
      }
      if (!await importer.ImportPackageListAsync(packageList, ct: cancellation.Token)) {
        return 4;
      }
    }
    else if (packageName is not null && packageVersion is not null) {
      if (!await importer.ImportSinglePackageAsync(packageName, packageVersion, cancellation.Token)) {
        return 5;
      }
    }
    else {
      // Should be impossible to get here.
      await logger.LogErrorAsync("Neither a package list nor a package id+version were provided.");
      return 6;
    }
    return 0;
  }

}
