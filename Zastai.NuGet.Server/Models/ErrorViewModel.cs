namespace Zastai.NuGet.Server.Models;

/// <summary>A model for a view for an error.</summary>
public class ErrorViewModel {

  /// <summary>The ID of the request that provoked the error, if available.</summary>
  public string? RequestId { get; set; }

  /// <summary>Indicates whether the request ID should be shown.</summary>
  public bool ShowRequestId => !string.IsNullOrEmpty(this.RequestId);

}
