using Babel.Application.Common;

namespace Babel.Application.Documents.Queries;

/// <summary>
/// Query para obtener el contenido de un documento para descarga.
/// </summary>
/// <param name="Id">ID del documento</param>
public sealed record GetDocumentContentQuery(Guid Id) : IQuery<DocumentContentResult>;

/// <summary>
/// Resultado de la query de contenido de documento.
/// </summary>
public sealed class DocumentContentResult
{
    public required Stream Content { get; init; }
    public required string FileName { get; init; }
    public required string MimeType { get; init; }
    public required long FileSizeBytes { get; init; }
}
