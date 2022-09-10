using NuGet.Common;

namespace Zastai.NuGet.PackageImport;

internal class SimpleConsoleLogger : ILogger {

  public const LogLevel DefaultMinimumLevel = LogLevel.Minimal;

  public LogLevel MinimumLevel { get; set; } = SimpleConsoleLogger.DefaultMinimumLevel;

  private static ConsoleColor ColorFor(LogLevel level) => level switch {
    LogLevel.Debug => ConsoleColor.Gray,
    LogLevel.Error => ConsoleColor.Red,
    LogLevel.Information => ConsoleColor.White,
    LogLevel.Minimal => ConsoleColor.Green,
    LogLevel.Verbose => ConsoleColor.Cyan,
    LogLevel.Warning => ConsoleColor.Yellow,
    _ => ConsoleColor.DarkGray
  };

  private static string PrefixFor(LogLevel level) => level switch {
    LogLevel.Debug => "debg",
    LogLevel.Error => "fail",
    LogLevel.Information => "info",
    LogLevel.Minimal => "info",
    LogLevel.Verbose => "trce",
    LogLevel.Warning => "warn",
    _ => "????"
  };

  private static TextWriter WriterFor(LogLevel level) => level switch {
    LogLevel.Error or LogLevel.Warning => Console.Error,
    _ => Console.Out
  };

  public void Log(ILogMessage message) {
    if (message.Level < this.MinimumLevel) {
      return;
    }
    this.Log(message.Level, $"[{message.Code}] {message.Message} ({message.ProjectPath})");
  }

  public void Log(LogLevel level, string data) {
    if (level < this.MinimumLevel) {
      return;
    }
    var color = Console.ForegroundColor;
    Console.ForegroundColor = SimpleConsoleLogger.ColorFor(level);
    var writer = SimpleConsoleLogger.WriterFor(level);
    try {
      writer.Write($"[{SimpleConsoleLogger.PrefixFor(level)}]");
    }
    finally {
      Console.ForegroundColor = color;
    }
    writer.Write(' ');
    writer.WriteLine(data);
  }

  public Task LogAsync(ILogMessage message) {
    if (message.Level < this.MinimumLevel) {
      return Task.CompletedTask;
    }
    return this.LogAsync(message.Level, $"[{message.Code}] {message.Message} ({message.ProjectPath})");
  }

  public async Task LogAsync(LogLevel level, string data) {
    if (level < this.MinimumLevel) {
      return;
    }
    var color = Console.ForegroundColor;
    Console.ForegroundColor = SimpleConsoleLogger.ColorFor(level);
    var writer = SimpleConsoleLogger.WriterFor(level);
    try {
      await writer.WriteAsync($"[{SimpleConsoleLogger.PrefixFor(level)}]");
    }
    finally {
      Console.ForegroundColor = color;
    }
    await writer.WriteAsync(' ');
    await writer.WriteLineAsync(data);
  }

  public void LogDebug(string data) => this.Log(LogLevel.Debug, data);

  public Task LogDebugAsync(string data) => this.LogAsync(LogLevel.Debug, data);

  public void LogError(string data) => this.Log(LogLevel.Error, data);

  public Task LogErrorAsync(string data) => this.LogAsync(LogLevel.Error, data);

  public void LogInformation(string data) => this.Log(LogLevel.Information, data);

  public Task LogInformationAsync(string data) => this.LogAsync(LogLevel.Information, data);

  public void LogInformationSummary(string data) => this.Log(LogLevel.Information, $"Summary: {data}");

  public Task LogInformationSummaryAsync(string data) => this.LogAsync(LogLevel.Information, $"Summary: {data}");

  public void LogMinimal(string data) => this.Log(LogLevel.Minimal, data);

  public Task LogMinimalAsync(string data) => this.LogAsync(LogLevel.Minimal, data);

  public void LogVerbose(string data) => this.Log(LogLevel.Verbose, data);

  public Task LogVerboseAsync(string data) => this.LogAsync(LogLevel.Verbose, data);

  public void LogWarning(string data) => this.Log(LogLevel.Warning, data);

  public Task LogWarningAsync(string data) => this.LogAsync(LogLevel.Warning, data);

}
