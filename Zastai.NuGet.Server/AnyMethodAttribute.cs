using Microsoft.AspNetCore.Mvc.Routing;

namespace Zastai.NuGet.Server;

/// <summary>Attribute marking an action method as supporting any HTTP method.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class AnyMethodAttribute : HttpMethodAttribute {

  /// <summary>Creates a new <see cref="AnyMethodAttribute"/>.</summary>
  public AnyMethodAttribute() : base(AnyMethodAttribute.AllMethods) {
  }

  /// <summary>"All" HTTP methods.</summary>
  private static readonly string[] AllMethods = {
    HttpMethod.Delete.Method, HttpMethod.Get.Method, HttpMethod.Head.Method, HttpMethod.Options.Method, HttpMethod.Patch.Method,
    HttpMethod.Post.Method, HttpMethod.Put.Method, HttpMethod.Trace.Method,
  };

}
