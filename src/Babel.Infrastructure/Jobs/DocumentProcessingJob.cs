using Babel.Application.Interfaces;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Babel.Infrastructure.Jobs;

/// <summary>
/// Job de Hangfire para procesar documentos (extracción de texto).
/// </summary>
public class DocumentProcessingJob
{
    private readonly ITextExtractionService _textExtractionService;
    private readonly ILogger<DocumentProcessingJob> _logger;

    public DocumentProcessingJob(
        ITextExtractionService textExtractionService,
        ILogger<DocumentProcessingJob> logger)
    {
        _textExtractionService = textExtractionService;
        _logger = logger;
    }

    /// <summary>
    /// Procesa un documento extrayendo su texto.
    /// </summary>
    /// <param name="documentId">ID del documento a procesar</param>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    [JobDisplayName("Extraer texto: {0}")]
    public async Task ProcessAsync(Guid documentId)
    {
        _logger.LogInformation("Starting document processing job for {DocumentId}", documentId);

        var result = await _textExtractionService.ExtractTextAsync(documentId);

        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Document {DocumentId} processed successfully, extracted {Length} characters",
                documentId, result.Value?.Length ?? 0);
        }
        else
        {
            if (result.Error.Code == "Document.RequiresOcr")
            {
                _logger.LogInformation(
                    "Document {DocumentId} requires OCR processing",
                    documentId);
                // TODO: Encolar job de OCR
            }
            else
            {
                _logger.LogWarning(
                    "Document {DocumentId} processing failed: {ErrorCode} - {ErrorMessage}",
                    documentId, result.Error.Code, result.Error.Description);
                throw new InvalidOperationException($"Document processing failed: {result.Error.Description}");
            }
        }
    }
}
