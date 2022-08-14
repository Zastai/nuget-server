using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

using JetBrains.Annotations;

namespace Zastai.NuGet.Server.Controllers.Documents;

/// <summary>Information about a resource in a NuGet v3 service index.</summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class ServiceIndexResource {

  /// <summary>Creates a new service index resource.</summary>
  /// <param name="uri">The ID (an absolute URI) of the resource.</param>
  /// <param name="type">The resource type; typically in the form "<c>NAME/VERSION</c>".</param>
  /// <param name="description">A description of the resource.</param>
  public ServiceIndexResource(Uri uri, string type, string description) {
    this.Uri = uri;
    this.Type = type;
    this.Description = description;
  }

  /// <summary>The ID (an absolute URI) of the resource.</summary>
  [JsonPropertyName("@id")]
  [Required]
  public Uri Uri { get; }

  /// <summary>The resource type; typically in the form "<c>NAME/VERSION</c>".</summary>
  [JsonPropertyName("@type")]
  [Required]
  public string Type { get; }

  /// <summary>A description of the resource.</summary>
  [JsonPropertyName("comment")]
  [Required]
  public string Description { get; }

}
