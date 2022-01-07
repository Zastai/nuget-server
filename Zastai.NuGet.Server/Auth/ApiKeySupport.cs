using Microsoft.AspNetCore.Authentication;

namespace Zastai.NuGet.Server.Auth;

/// <summary>Helper methods and constants for API Key support.</summary>
public static class ApiKeySupport {

  /// <summary>The name of the request header that is expected to contain the API key.</summary>
  public const string Header = "X-NuGet-ApiKey";

  /// <summary>The name of the API key authentication scheme.</summary>
  public const string Scheme = "API Key";

  /// <summary>Adds API key authentication support.</summary>
  /// <param name="authenticationBuilder">The authentication builder to add API key authentication support to.</param>
  /// <param name="options">The code needed to set up the appropriate options.</param>
  /// <returns><paramref name="authenticationBuilder"/>, with API key authentication support added.</returns>
  public static AuthenticationBuilder AddApiKeySupport(this AuthenticationBuilder authenticationBuilder,
                                                       Action<ApiKeyAuthenticationOptions>? options = null)
    => authenticationBuilder.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(ApiKeySupport.Scheme, options);

}
