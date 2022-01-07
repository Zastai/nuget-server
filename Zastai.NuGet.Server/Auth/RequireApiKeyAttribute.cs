using Microsoft.AspNetCore.Authorization;

namespace Zastai.NuGet.Server.Auth;

/// <summary>Marks an action or controller as requiring an API key (<c>X-NuGet-ApiKey</c> header).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireApiKeyAttribute : AuthorizeAttribute {

  /// <summary>Creates a new attribute to mark an action or controller as requiring an API key.</summary>
  public RequireApiKeyAttribute() {
    this.AuthenticationSchemes = ApiKeySupport.Scheme;
  }

}
