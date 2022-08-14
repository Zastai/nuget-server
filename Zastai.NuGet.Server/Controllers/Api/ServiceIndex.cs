using Microsoft.AspNetCore.Mvc;

namespace Zastai.NuGet.Server.Controllers.Api;

/// <summary>A NuGet v3 service index.</summary>
[Route(ServiceIndex.BasePath)]
public sealed class ServiceIndex : ApiController<ServiceIndex> {

  /// <summary>Creates a new NuGet v3 service index controller.</summary>
  /// <param name="logger">A logger for the controller.</param>
  public ServiceIndex(ILogger<ServiceIndex> logger) : base(logger) {
  }

  private const string BasePath = "api/v3";

  /// <summary>Gets the NuGet v3 service index.</summary>
  /// <returns>The NuGet v3 service index.</returns>
  /// <response code="200">Always.</response>
  [HttpGet("index.json")]
  public Documents.ServiceIndex Get() {
    // FIXME: Would it be safe to cache this? Or can the scheme/host/port differ between requests?
    var req = this.Request;
    var baseUri = new UriBuilder(req.Scheme, req.Host.Host, req.Host.Port ?? -1).Uri;
    return new Documents.ServiceIndex(baseUri);
  }

}
