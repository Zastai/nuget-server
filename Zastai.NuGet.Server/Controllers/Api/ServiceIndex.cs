using System.Text.Json.Serialization;

using JetBrains.Annotations;

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

  /// <summary>A NuGet v3 service index document.</summary>
  [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
  public sealed class Document {

    private const string SchemaVersion = "3.0.0";

    /// <summary>The schema version for this service index document.</summary>
    [JsonPropertyName("version")]
    public string Version { get; } = Document.SchemaVersion;

    /// <summary>Information about a resource in a NuGet v3 service index.</summary>
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public interface IResource {

      /// <summary>The ID (an absolute URI) of the resource.</summary>
      [JsonPropertyName("@id")]
      Uri Uri { get; }

      /// <summary>The resource type; typically in the form "<c>NAME/VERSION</c>".</summary>
      [JsonPropertyName("@type")]
      string Type { get; }

      /// <summary>A description of the resource.</summary>
      [JsonPropertyName("comment")]
      string Description { get; }

    }

    /// <summary>The resources defined in the service index.</summary>
    [JsonPropertyName("resources")]
    public IEnumerable<IResource> Resources { get; }

    /// <summary>Creates a new NuGet v3 service index document.</summary>
    /// <param name="baseUri">The base URI for resources with a relative path.</param>
    public Document(Uri baseUri) {
      var resources = new List<Resource>();
      Document.Add(resources, baseUri, Autocomplete.Service);
      Document.Add(resources, baseUri, Catalog.Service);
      Document.Add(resources, baseUri, PackageDownload.Service);
      Document.Add(resources, baseUri, PackageDetails.Service);
      Document.Add(resources, baseUri, PackageMetadata.Service);
      Document.Add(resources, baseUri, PublishPackage.Service);
      Document.Add(resources, baseUri, PublishSymbols.Service);
      Document.Add(resources, baseUri, ReportAbuse.Service);
      Document.Add(resources, baseUri, RepositorySignatures.Service);
      Document.Add(resources, baseUri, Search.Service);
      Document.Add(resources, baseUri, SymbolServer.Service);
      this.Resources = resources;
    }

    private static void Add(ICollection<Resource> resources, Uri baseUri, NuGetService service) {
      foreach (var type in service.Types) {
        resources.Add(new Resource(new Uri(baseUri, service.Path), type, service.Description));
      }
    }

    private sealed record Resource(Uri Uri, string Type, string Description) : IResource;

  }

  /// <summary>Gets the NuGet v3 service index.</summary>
  /// <returns>The NuGet v3 service index.</returns>
  /// <response code="200">Always.</response>
  [HttpGet("index.json")]
  public Document Get() {
    // FIXME: Would it be safe to cache this? Or can the scheme/host/port differ between requests?
    var req = this.Request;
    var baseUri = new UriBuilder(req.Scheme, req.Host.Host, req.Host.Port ?? -1).Uri;
    return new Document(baseUri);
  }

}
