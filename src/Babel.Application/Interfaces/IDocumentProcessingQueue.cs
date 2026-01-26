namespace Babel.Application.Interfaces;

/// <summary>
/// Cola para encolar documentos para procesamiento en segundo plano.
/// </summary>
public interface IDocumentProcessingQueue
{
    /// <summary>
    /// Encola un documento para extracción de texto.
    /// </summary>
    /// <param name="documentId">ID del documento a procesar</param>
    /// <returns>ID del trabajo encolado</returns>
    string EnqueueTextExtraction(Guid documentId);

    /// <summary>
    /// Encola un documento para procesamiento OCR.
    /// </summary>
    /// <param name="documentId">ID del documento a procesar</param>
    /// <returns>ID del trabajo encolado</returns>
    string EnqueueOcrProcessing(Guid documentId);

    /// <summary>
    /// Encola un documento para vectorización.
    /// </summary>
    /// <param name="documentId">ID del documento a vectorizar</param>
    /// <returns>ID del trabajo encolado</returns>
    string EnqueueVectorization(Guid documentId);
}
