using System.Reflection.Metadata;

namespace Zastai.NuGet.Server;

/// <summary>Utilities for working with the symbols store.</summary>
public static class SymbolStore {

  /// <summary>Gets the signature string for a (portable) PDB file.</summary>
  /// <param name="stream">A stream containing the the PDB file to get the signature for.</param>
  /// <returns>The PDB signature; this is the string that will be used by a symbol store client to request that PDB.</returns>
  public static string GetSignature(Stream stream) {
    // FIXME: Maybe this could/should be extended to also work for native PDB files.
    using var provider = MetadataReaderProvider.FromPortablePdbStream(stream);
    var reader = provider.GetMetadataReader();
    if (reader.DebugMetadataHeader is null) {
      throw new InvalidDataException("No PDB header found.");
    }
    var id = reader.DebugMetadataHeader.Id;
    if (id.Length != 20) {
      throw new InvalidDataException($"PDB header has incorrectly-sized ID ({id.Length} != 20.");
    }
    // The ID field here consists of 20 bytes: a 16-byte GUID and a 4-byte "age" value.
    // The age value does not seem to correspond to any real date/time value.
    // The symbol store processing seems to use the upper case version of the GUID part only (without hyphens), followed by '1'.
    // It's unclear why it does not simply use the hex form of the 20 ID bytes.
    var guid = new Guid(id.AsSpan()[..16]);
    return guid.ToString("N").ToUpperInvariant() + "1";
  }

  /// <summary>Gets the signature string for a (portable) PDB file.</summary>
  /// <param name="path">The path the the PDB file to get the signature for.</param>
  /// <returns>The PDB signature; this is the string that will be used by a symbol store client to request that PDB.</returns>
  public static string GetSignature(string path) {
    using var fs = File.OpenRead(path);
    return SymbolStore.GetSignature(fs);
  }

  /// <summary>Attempts to open a symbol file (.pdb) with a particular signature.</summary>
  /// <param name="name">The name of the requested symbol file (without extension).</param>
  /// <param name="signature">The requested signature.</param>
  /// <returns>A stream for reading the requested symbol file, or <see langword="null"/> if it is not available.</returns>
  /// <exception cref="InvalidOperationException">
  /// When the symbol file is found, but its signature does not match the requested signature.
  /// </exception>
  public static Stream? Open(string name, string signature) {
    string? path = null;
    // TODO: Compose actual path.
    if (path is null) {
      return null;
    }
    path = Path.Combine(path, name + ".pdb");
    if (!File.Exists(path)) {
      return null;
    }
    var actualSignature = SymbolStore.GetSignature(path);
    if (signature == actualSignature) {
      return File.OpenRead(path);
    }
    var msg = $"Found a PDB file for signature '{signature}' but it has a different signature ('{actualSignature}').";
    throw new InvalidOperationException(msg);
  }

}
