using Microsoft.AspNetCore.Mvc;

using Zastai.NuGet.Server.Services;

namespace Zastai.NuGet.Server.Controllers.Api;

/// <summary>A Microsoft Symbol Store over HTTP(s), serving PDB files taken from pushed symbol packages.</summary>
[Route(SymbolServer.BasePath)]
public sealed class SymbolServer : ApiController<SymbolServer> {

  private const string BasePath = "symbols";

  private const string SymbolFileContentType = "application/octet-stream";

  /// <summary>Creates a new symbol server controller.</summary>
  /// <param name="logger">A logger for the controller.</param>
  /// <param name="symbolStore">The symbol store to use.</param>
  public SymbolServer(ILogger<SymbolServer> logger, ISymbolStore symbolStore) : base(logger) {
    this._symbolStore = symbolStore;
  }

  private readonly ISymbolStore _symbolStore;

  /// <summary>Retrieve (the contents of) a compressed PDB file.</summary>
  /// <remarks>
  /// This will be usually be requested after a request for an uncompressed PDB file has failed. We don't bother compressing symbol
  /// files, so this method always fails.
  /// </remarks>
  /// <param name="name">The name of the PDB file for this request (without extension).</param>
  /// <param name="signature">The signature of the PDB file for this request.</param>
  /// <param name="file">
  /// The name of the file being requested (without extension); this should match <paramref name="name"/>.
  /// </param>
  /// <returns><c>404 - NOT FOUND</c></returns>
  /// <response code="404">Always.</response>
  [HttpGet("{name}.pdb/{signature}/{file}.pd_")]
  [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
  public NotFoundResult GetCompressedSymbolFile(string name, string signature, string file) {
    if (file != name) {
      this.Logger.LogWarning("Request with inconsistent symbol file name (\"{name}.pdb\" vs \"{file}.pd_\").", name, file);
      return this.NotFound();
    }
    this.Logger.LogWarning("Asked for compressed symbols (PD_ file) for {name} (signature: {signature}).", name, signature);
    return this.NotFound();
  }

  /// <summary>Retrieve a PDB file.</summary>
  /// <param name="name">The name of the PDB file for this request (without extension).</param>
  /// <param name="signature">The signature of the PDB file for this request.</param>
  /// <param name="file">
  /// The name of the file being requested (without extension); this should match <paramref name="name"/>.
  /// </param>
  /// <returns>The requested PDB file if it is available; <c>404 - NOT FOUND</c> otherwise.</returns>
  /// <response code="200">When the requested PDB file is being returned.</response>
  /// <response code="404">When the requested PDB file is not available.</response>
  [HttpGet("{name}.pdb/{signature}/{file}.pdb")]
  [ProducesResponseType(typeof(IEnumerable<byte>), StatusCodes.Status200OK, SymbolServer.SymbolFileContentType)]
  [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
  public IActionResult GetSymbolFile(string name, string signature, string file) {
    if (file != name) {
      this.Logger.LogWarning("Request with inconsistent symbol file name (\"{name}.pdb\" vs \"{file}.pdb\").", name, file);
      return this.NotFound();
    }
    var pdb = this._symbolStore.Open(name, signature);
    if (pdb is null) {
      this.Logger.LogWarning("Asked for symbols (PDB file) for {name} (signature: {signature}), but they were not available.", name,
                             signature);
      return this.NotFound();
    }
    this.Logger.LogWarning("Returning symbols (PDB file) for {name} (signature: {signature}).", name, signature);
    return this.File(pdb, SymbolServer.SymbolFileContentType, $"{name}.pdb");
  }

  /// <summary>Retrieve (the contents) of a redirection/pointer file for a PDB file.</summary>
  /// <remarks>
  /// This will be usually be requested after requests for both uncompressed and compressed PDB files have failed. We handle
  /// redirection ourselves as part of the uncompressed PDB retrieval, so this method always fails.
  /// </remarks>
  /// <param name="name">The name of the PDB file for this request (without extension).</param>
  /// <param name="signature">The signature of the PDB file for this request.</param>
  /// <returns><c>404 - NOT FOUND</c></returns>
  /// <response code="404">Always.</response>
  [HttpGet("{name}.pdb/{signature}/file.ptr")]
  [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
  public NotFoundResult GetSymbolPointer(string name, string signature) {
    this.Logger.LogWarning("Asked for pointer to symbols for {name} (signature: {signature}).", name, signature);
    return this.NotFound();
  }

  /// <summary>This intercepts requests for "index2.txt".</summary>
  /// <remarks>
  /// If this is considered found, the client will make requests for a two-tier structure, using the first two characters of the
  /// PDB file name as an initial path element (i.e. asking for "fo/foo.pdb/xxx" instead of just "foo.pdb/xxx".<br/>
  /// Because we potentially remap the PDB file anyway, that extra split has no benefit at all, so we consider "index2.txt" not to
  /// exist.
  /// </remarks>
  /// <returns><c>404 - NOT FOUND</c></returns>
  /// <response code="404">Always.</response>
  [HttpGet("index2.txt")]
  [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
  public NotFoundResult NoTwoTierStructure() {
    this.Logger.LogInformation("Ignored request for index2.txt.");
    return this.NotFound();
  }

  #region NuGet Service Info

  // This is not technically a NuGet service. But there's no harm in including it in the index.

  private const string Description = "A symbol server for PDB files associated with NuGet packages.";

  private static readonly string[] Types = {
    "SymbolServer/1.0.0",
  };

  /// <summary>NuGet service information for the symbol server.</summary>
  public static readonly NuGetService Service = new(SymbolServer.BasePath, SymbolServer.Types, SymbolServer.Description);

  #endregion

}
