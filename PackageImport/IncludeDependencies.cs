namespace Zastai.NuGet.PackageImport;


/// <summary>How to handle dependencies of a package.</summary>
internal enum IncludeDependencies {

  /// <summary>Do not import dependencies.</summary>
  None,

  /// <summary>Import the best matching version of each dependency.</summary>
  BestMatch,

  /// <summary>Import all matching versions of each dependency.</summary>
  All,

}
