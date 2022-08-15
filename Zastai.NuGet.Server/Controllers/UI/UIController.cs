using Microsoft.AspNetCore.Mvc;

using Zastai.NuGet.Server.Services;

namespace Zastai.NuGet.Server.Controllers.UI;

/// <summary>A UI (view) controller, with a dedicated logger and a default route for logging purposes.</summary>
/// <typeparam name="T">The specific type of controller.</typeparam>
[ApiExplorerSettings(IgnoreApi = true)]
[Produces("text/html")]
[ServiceFilter(typeof(RequestLoggingFilter))]
public abstract class UIController<T> : Controller where T : UIController<T> {

  /// <summary>Creates a new UI controller.</summary>
  /// <param name="logger">A logger for the controller.</param>
  protected UIController(ILogger<T> logger) {
    this.Logger = logger;
  }

  /// <summary>The logger for this controller.</summary>
  protected readonly ILogger<T> Logger;

  /// <summary>An action that logs otherwise-unhandled requests under the scope of this controller's routing prefix.</summary>
  /// <param name="resource">The resource being requested.</param>
  /// <returns><c>404 - NOT FOUND</c></returns>
  /// <response code="404">Always.</response>
  [AnyMethod]
  [ApiExplorerSettings(IgnoreApi = true)]
  [Route("{*resource}", Order = 999)]
  public NotFoundResult LogUnhandledRequest(string? resource) {
    var req = this.Request;
    this.Logger.LogWarning("Unhandled {method} for '{path}'.", req.Method, req.Path);
    this.Logger.LogInformation("Request Headers: {headers}", req.Headers);
    return this.NotFound();
  }

}
