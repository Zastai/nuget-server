using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

using JetBrains.Annotations;

namespace Zastai.NuGet.Server.Controllers.Documents;

/// <summary>Information about a certificate used to sign packages.</summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public sealed class SigningCertificate {

  /// <summary>Creates information about a certificate used to sign packages.</summary>
  /// <param name="contentUrl">Absolute URL to the DER-encoded public certificate.</param>
  /// <param name="fingerprints">The certificate's fingerprints.</param>
  /// <param name="issuer">The distinguished name of the certificate's issuer.</param>
  /// <param name="notAfter">The ending timestamp of the certificate's validity period.</param>
  /// <param name="notBefore">The starting timestamp of the certificate's validity period.</param>
  /// <param name="subject">The subject distinguished name from the certificate.</param>
  public SigningCertificate(Uri contentUrl, CertificateFingerprints fingerprints, string issuer, DateTimeOffset notAfter,
                            DateTimeOffset notBefore, string subject) {
    this.ContentUrl = contentUrl;
    this.Fingerprints = fingerprints;
    this.Issuer = issuer;
    this.NotAfter = notAfter;
    this.NotBefore = notBefore;
    this.Subject = subject;
  }

  /// <summary>Absolute URL to the DER-encoded public certificate.</summary>
  [JsonPropertyName("contentUrl")]
  [Required]
  public Uri ContentUrl { get; }

  /// <summary>The certificate's fingerprints.</summary>
  [JsonPropertyName("fingerprints")]
  [Required]
  public CertificateFingerprints Fingerprints { get; }

  /// <summary>The distinguished name of the certificate's issuer.</summary>
  [JsonPropertyName("issuer")]
  [Required]
  public string Issuer { get; }

  /// <summary>The ending timestamp of the certificate's validity period.</summary>
  [JsonPropertyName("notAfter")]
  [Required]
  public DateTimeOffset NotAfter { get; }

  /// <summary>The starting timestamp of the certificate's validity period.</summary>
  [JsonPropertyName("notBefore")]
  [Required]
  public DateTimeOffset NotBefore { get; }

  /// <summary>The subject distinguished name from the certificate.</summary>
  [JsonPropertyName("subject")]
  [Required]
  public string Subject { get; }

}
