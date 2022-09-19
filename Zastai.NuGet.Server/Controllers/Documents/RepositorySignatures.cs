using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

using JetBrains.Annotations;

namespace Zastai.NuGet.Server.Controllers.Documents;

/// <summary>A NuGet repository signatures document.</summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed class RepositorySignatures {

  /// <summary>Indicates whether or not all packages stored in the server are signed.</summary>
  [JsonPropertyName("allRepositorySigned")]
  [Required]
  public bool AllPackagesAreSigned { get; }

  /// <summary>The signing certificates used by the server.</summary>
  [JsonPropertyName("signingCertificates")]
  [Required]
  public SigningCertificate[] Certificates { get; } = Array.Empty<SigningCertificate>();

}
