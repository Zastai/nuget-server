using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Zastai.NuGet.Server;

/// <summary>Marks an action or controller as requiring an API key (<c>X-NuGet-ApiKey</c> header).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireApiKey : Attribute, IAsyncActionFilter {

  private const string ApiKeyHeader = "X-NuGet-ApiKey";

  /// <inheritdoc />
  public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next) {
    var services = context.HttpContext.RequestServices;
    var logger = services.GetRequiredService<ILogger<RequireApiKey>>();
    var req = context.HttpContext.Request;
    var headers = req.Headers;
    if (!headers.TryGetValue(RequireApiKey.ApiKeyHeader, out var apiKeys)) {
      logger.LogWarning("No API key specified for {m} {p}", req.Method, req.Path);
      context.Result = new UnauthorizedResult();
      return;
    }
    if (apiKeys.Count != 1) {
      logger.LogWarning("Too many API keys ({c}) specified for {m} {p}", apiKeys.Count, req.Method, req.Path);
      context.Result = new UnauthorizedResult();
      return;
    }
    var apiKey = apiKeys[0];
    // TODO: Validate the API key.
    logger.LogInformation("Ignoring API key ({k}) specified for {m} {p}", apiKey, req.Method, req.Path);
    await next();
  }

}
