namespace Zastai.NuGet.Server.Services;

/// <summary>A symbol store.</summary>
public interface ISymbolStore {

  /// <summary>Adds a PDB file to the store.</summary>
  /// <param name="name">The name of the symbol file.</param>
  /// <param name="stream">A stream containing the PDB file to add.</param>
  Task AddSymbolFileAsync(string name, Stream stream);

  /// <summary>Gets the signature string for a PDB file.</summary>
  /// <param name="stream">A stream containing the PDB file to get the signature for.</param>
  /// <returns>The PDB signature; this is the string that will be used by a symbol store client to request that PDB.</returns>
  string GetSignature(Stream stream);

  /// <summary>Gets the signature string for a PDB file.</summary>
  /// <param name="path">The path the PDB file to get the signature for.</param>
  /// <returns>The PDB signature; this is the string that will be used by a symbol store client to request that PDB.</returns>
  string GetSignature(string path);

  /// <summary>Attempts to open a symbol file (.pdb) with a particular signature.</summary>
  /// <param name="name">The name of the requested symbol file (without extension).</param>
  /// <param name="signature">The requested signature.</param>
  /// <returns>A stream for reading the requested symbol file, or <see langword="null"/> if it is not available.</returns>
  /// <exception cref="InvalidOperationException">
  /// When the symbol file is found, but its signature does not match the requested signature.
  /// </exception>
  public Stream? Open(string name, string signature);

}
