using System.Reflection.Metadata;
using System.Text;

namespace Zastai.NuGet.Server;

/// <summary>Utilities for working with the symbols store.</summary>
public static class SymbolStore {

  private static readonly byte[] NativePDBHeaderMagic = Encoding.ASCII.GetBytes("Microsoft C/C++ MSF 7.00\r\n\x001ADS\0\0\0");

  private static readonly byte[] NativePDBPageMagic = { 0x94, 0x2e, 0x31, 0x01 };

  private static Guid? GetNativeSignature(Stream stream) {
    {
      var magic = new byte[32];
      if (stream.Read(magic) != 32 || !SymbolStore.NativePDBHeaderMagic.SequenceEqual(magic)) {
        return null;
      }
    }
    // This is followed by:
    // I32 page size (in bytes)
    // I32 free page map
    // I32 pages used
    // I32 directory size (in bytes)
    // I32 zero
    // I32 page numbers for the directory
    //     MS calculation for # of directory pages: (((directorySize + pageSize - 1) / pageSize * 4) + pageSize - 1) / pageSize
    //
    // I can't find any documentation for the directory contents; the root page(s) referred to by that last set of I32 values seem
    // to contain page numbers themselves (i.e. 2 two-tier collection of page pointers).
    // While I can easily find the page that includes the GUID signature by looking for that GUID, it is far from clear how I can
    // get there without that prior knowledge. In the files I've looked at, the number of that page only occurred in one place in
    // the file, inside a page reachable from the directory, but not at a fixed location within that page, and with no obvious
    // common value before/after it. Those pages also clearly contained non-page-number contents, with no immediately obvious
    // pattern.
    // However, these pages always seem to precede the directory pages and start with the same 4 bytes (X'942E3101'), so for now
    // the logic is:
    // - read header fields and directory page pointers
    // - stop if the zero field is not zero
    // - seek to the first directory page
    // - get the first "subdirectory" page number from it and seek to the page before it
    // - walk pages backward until one is found that starts with the required value
    //   - if found, get the GUID from it
    //     - the GUID starts at offset 12 in the page. In the files I've seen, it's preceded by an I32 containing 1, but it's not
    //       clear whether that's something that can be used for validation or not.
    using var br = new BinaryReader(stream, Encoding.ASCII, true);
    var pageSize = br.ReadInt32();
    stream.Position += 12;
    var zero = br.ReadInt32();
    if (zero != 0) {
      return null;
    }
    {
      var firstDirectoryPage = br.ReadInt32();
      stream.Position = firstDirectoryPage * pageSize;
      var firstSubdirectoryPage = br.ReadInt32();
      if (firstDirectoryPage <= 1) {
        return null;
      }
      stream.Position = (firstSubdirectoryPage - 1) * pageSize;
    }
    {
      var magic = new byte[4];
      var pos = stream.Position;
      for (; pos >= pageSize; stream.Position = pos - pageSize) {
        pos = stream.Position;
        if (stream.Read(magic) != 4 || !SymbolStore.NativePDBPageMagic.SequenceEqual(magic)) {
          continue;
        }
        stream.Position += 8;
        var guid = new byte[16];
        if (stream.Read(guid) != 16) {
          return null;
        }
        return new Guid(guid);
      }
    }
    return null;
  }

  private static Guid? GetPortableSignature(Stream stream) {
    try {
      using var provider = MetadataReaderProvider.FromPortablePdbStream(stream, MetadataStreamOptions.LeaveOpen);
      var reader = provider.GetMetadataReader();
      if (reader.DebugMetadataHeader is null) {
        throw new InvalidDataException("No PDB header found.");
      }
      var id = reader.DebugMetadataHeader.Id;
      if (id.Length != 20) {
        throw new InvalidDataException($"PDB header has incorrectly-sized ID ({id.Length} != 20).");
      }
      // The ID field here consists of 20 bytes: a 16-byte GUID followed by 4 bytes that may or may not be some sort of "age" (but
      // they do not seem to correspond to any real date/time value).
      // The symbol store processing only seems to use the GUID part.
      return new Guid(id.AsSpan()[..16]);
    }
    catch (BadImageFormatException) when (stream.CanSeek) {
      return null;
    }
  }

  /// <summary>Gets the signature string for a (portable) PDB file.</summary>
  /// <param name="stream">A stream containing the the PDB file to get the signature for.</param>
  /// <returns>The PDB signature; this is the string that will be used by a symbol store client to request that PDB.</returns>
  public static string GetSignature(Stream stream) {
    var startPosition = stream.Position;
    // Assume Portable PDB format, for which the framework provides handling.
    var guid = SymbolStore.GetPortableSignature(stream);
    if (guid is null && stream.CanSeek) { // Otherwise, if we can rewind, try to handle it as a native PDB
      stream.Position = startPosition;
      guid = SymbolStore.GetNativeSignature(stream);
    }
    if (guid is null) {
      throw new BadImageFormatException("Failed to extract the symbol file signature.");
    }
    return guid.Value.ToString("N").ToUpperInvariant() + "1";
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
