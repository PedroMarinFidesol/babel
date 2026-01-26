using Babel.Application.Common;

namespace Babel.Application.Interfaces;

/// <summary>
/// Servicio para extraer texto de documentos.
/// </summary>
public interface ITextExtractionService
{
    /// <summary>
    /// Extrae el texto de un documento según su tipo.
    /// </summary>
    /// <param name="documentId">ID del documento a procesar</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Resultado con el texto extraído o error</returns>
    Task<Result<string>> ExtractTextAsync(Guid documentId, CancellationToken cancellationToken = default);
}
