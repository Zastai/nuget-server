namespace Zastai.NuGet.Server.Services;

/// <summary>Settings used by the NuGet server.</summary>
public interface ISettings {

  /// <summary>Indicates whether deleting a package is allowed.</summary>
  bool IsDeleteAllowed { get; }

  /// <summary>Indicates whether relisting a package is allowed.</summary>
  bool IsRelistAllowed { get; }

  /// <summary>Indicates whether unlisting a package is allowed.</summary>
  bool IsUnlistAllowed { get; }

}
