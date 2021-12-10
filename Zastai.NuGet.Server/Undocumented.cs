using Zastai.NuGet.Server.Controllers.Api;

namespace Zastai.NuGet.Server;

/// <summary>Undocumented NuGet services observed in the Service Index of nuget.org.</summary>
/// <remarks>Controllers for these can be added if a client can be found that relies on them.</remarks>
public static class Undocumented {

  /// <summary>Another search endpoint. Unclear what this is for (other than perhaps internal nuget.org use).</summary>
  public static class GallerySearch {

    private const string BasePath = "search/gallery";

    private const string Description = "Search Service used by Gallery.";

    private static readonly string[] Types = {
      "SearchGalleryQueryService/3.0.0-rc",
    };

    /// <summary>NuGet service information.</summary>
    public static readonly NuGetService Service = new(GallerySearch.BasePath, GallerySearch.Types, GallerySearch.Description);

  }

  /// <summary>The legacy NuGet v2 API prefix. Used by the NuGet command-line client for commands like <c>list</c>.</summary>
  public static class LegacyGallery {

    private const string BasePath = "api/v2";

    private const string Description = "Legacy gallery.";

    private static readonly string[] Types = {
      "LegacyGallery",
      "LegacyGallery/2.0.0",
    };

    /// <summary>NuGet service information.</summary>
    public static readonly NuGetService Service = new(LegacyGallery.BasePath, LegacyGallery.Types, LegacyGallery.Description);

  }

  /// <summary>This points inside the prefix of <see cref="PackageMetadata"/>; unclear whether or not this is used.</summary>
  public static class PackageDisplayMetadata {

    private const string BasePath = PackageMetadata.BasePath + "/{id-lower}/index.json";

    private const string Description = "URI template used by NuGet Client to construct display metadata for Packages using ID.";

    private static readonly string[] Types = {
      "PackageDisplayMetadataUriTemplate/3.0.0-rc",
    };

    /// <summary>NuGet service information.</summary>
    public static readonly NuGetService Service = new(PackageDisplayMetadata.BasePath, PackageDisplayMetadata.Types,
                                                      PackageDisplayMetadata.Description);

  }

  /// <summary>This points inside the prefix of <see cref="PackageMetadata"/>; unclear whether or not this is used.</summary>
  public static class PackageVersionDisplayMetadata {

    private const string BasePath = PackageMetadata.BasePath + "/{id-lower}/{version-lower}/index.json";

    private const string Description =
      "URI template used by NuGet Client to construct display metadata for Packages using ID, Version.";

    private static readonly string[] Types = {
      "PackageVersionDisplayMetadataUriTemplate/3.0.0-rc",
    };

    /// <summary>NuGet service information.</summary>
    public static readonly NuGetService Service = new(PackageVersionDisplayMetadata.BasePath, PackageVersionDisplayMetadata.Types,
                                                      PackageVersionDisplayMetadata.Description);

  }

}
