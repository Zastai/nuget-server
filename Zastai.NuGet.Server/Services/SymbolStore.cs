using System.Reflection.Metadata;
using System.Text;

namespace Zastai.NuGet.Server.Services;

/// <summary>A symbol store.</summary>
public class SymbolStore : ISymbolStore {

  /// <summary>Creates a new symbol store.</summary>
  /// <param name="logger">A logger for the symbol store.</param>
  /// <param name="host">The host environment.</param>
  public SymbolStore(ILogger<SymbolStore> logger, IHostEnvironment host) {
    this._logger = logger;
    this._symbolFolder = Path.Combine(host.ContentRootPath, "data", "symbols");
    this._tempFolder = Path.Combine(host.ContentRootPath, "temp", "symbols");
  }

  #region ISymbolStore

  /// <inheritdoc />
  public async Task AddSymbolFileAsync(string name, Stream stream) {
    // The stream is probably a DeflateStream from the nupkg - but those don't support positioning, and we need that.
    // So, save it to a temp file first.
    await this.WithTempFile(stream, async tempStream => {
      var signature = this.GetSignature(tempStream);
      var path = this.GetSymbolFilePath(name, signature);
      this._logger.LogInformation("Adding symbol file: {pdb}.", path);
      var dir = Path.GetDirectoryName(path);
      if (dir is not null) {
        Directory.CreateDirectory(dir);
      }
      await using var pdb = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
      tempStream.Position = 0;
      await tempStream.CopyToAsync(pdb);
    });
  }

  /// <inheritdoc />
  public string GetSignature(Stream stream) {
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

  /// <inheritdoc />
  public string GetSignature(string path) {
    using var fs = File.OpenRead(path);
    return this.GetSignature(fs);
  }

  /// <inheritdoc />
  public Stream? Open(string name, string signature) {
    var path = this.GetSymbolFilePath(name, signature);
    if (!File.Exists(path)) {
      return null;
    }
    var actualSignature = this.GetSignature(path);
    if (signature == actualSignature) {
      return File.OpenRead(path);
    }
    var msg = $"Found a PDB file for signature '{signature}' but it has a different signature ('{actualSignature}').";
    throw new InvalidOperationException(msg);
  }

  #endregion

  #region Internals

  private static readonly byte[] NativePDBHeaderMagic = Encoding.ASCII.GetBytes("Microsoft C/C++ MSF 7.00\r\n\x001ADS\0\0\0");

  private static readonly byte[] NativePDBPageMagic = { 0x94, 0x2e, 0x31, 0x01 };

  private readonly ILogger<SymbolStore> _logger;

  private readonly string _symbolFolder;

  private readonly string _tempFolder;

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

  private string GetSymbolFileDirectory(string name) {
    var dot = name.IndexOf('.');
    return dot switch {
      // System.Text.Json.pdb -> S\System\Text.Json
      > 0 => Path.Combine(this.SymbolFolder, name[..1], name.Remove(dot), name[(dot + 1)..]),
      // foo.pdb -> f\foo
      _ => Path.Combine(this.SymbolFolder, name[..1], name)
    };
  }

  private string GetSymbolFilePath(string name, string signature)
    => Path.Combine(this.GetSymbolFileDirectory(name), signature, name + ".pdb");

  private string SymbolFolder {
    get {
      Directory.CreateDirectory(this._symbolFolder);
      return this._symbolFolder;
    }
  }

  private string TempFolder {
    get {
      Directory.CreateDirectory(this._tempFolder);
      return this._tempFolder;
    }
  }

  private async Task WithTempFile(Stream pdb, Func<Stream, Task> code) {
    var fileName = Path.Combine(this.TempFolder, Path.GetRandomFileName());
    this._logger.LogInformation("Temporarily storing symbol file in {fileName}.", fileName);
    try {
      await using var stream = new FileStream(fileName, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
      await pdb.CopyToAsync(stream);
      await stream.FlushAsync();
      stream.Position = 0;
      await code(stream);
    }
    finally {
      try {
        File.Delete(fileName);
      }
      catch (Exception e) {
        this._logger.LogWarning("Unable to delete temporary file ({f}): {e}", fileName, e);
      }
    }
  }

  #endregion

}
