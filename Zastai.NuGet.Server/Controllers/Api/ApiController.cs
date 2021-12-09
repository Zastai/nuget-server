using Microsoft.AspNetCore.Mvc;

namespace Zastai.NuGet.Server.Controllers.Api;

/// <summary>An API controller, with a dedicated logger and a default route for logging purposes.</summary>
/// <typeparam name="T">The specific type of controller.</typeparam>
[ApiController]
[Produces("application/json")]
public abstract class ApiController<T> : Controller where T : ApiController<T> {

  /// <summary>Creates a new API controller.</summary>
  /// <param name="logger">A logger for the controller.</param>
  protected ApiController(ILogger<T> logger) {
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
  [Produces("text/html")]
  [Route("{*resource}", Order = 999)]
  public NotFoundResult LogUnhandledRequest(string? resource) {
    var req = this.Request;
    this.Logger.LogWarning("Unhandled {method} for '{path}'.", req.Method, req.Path);
    this.Logger.LogInformation("Request Headers: {headers}", req.Headers);
    return this.NotFound();
  }

}
