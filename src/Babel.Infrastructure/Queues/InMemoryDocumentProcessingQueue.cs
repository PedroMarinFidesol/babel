using Babel.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Babel.Infrastructure.Queues;

/// <summary>
/// Implementación no-op de IDocumentProcessingQueue.
/// Se usa como fallback cuando Hangfire no está configurado.
/// ADVERTENCIA: No ejecuta ningún procesamiento real.
/// </summary>
public class InMemoryDocumentProcessingQueue : IDocumentProcessingQueue
{
    private readonly ILogger<InMemoryDocumentProcessingQueue> _logger;

    public InMemoryDocumentProcessingQueue(ILogger<InMemoryDocumentProcessingQueue> logger)
    {
        _logger = logger;
    }

    public string EnqueueTextExtraction(Guid documentId)
    {
        _logger.LogWarning(
            "InMemoryDocumentProcessingQueue: EnqueueTextExtraction llamado para {DocumentId}. " +
            "Hangfire no está configurado - el documento NO será procesado. " +
            "Configure ConnectionStrings:HangfireConnection o ConnectionStrings:DefaultConnection.",
            documentId);
        return Guid.NewGuid().ToString();
    }

    public string EnqueueOcrProcessing(Guid documentId)
    {
        _logger.LogWarning(
            "InMemoryDocumentProcessingQueue: EnqueueOcrProcessing llamado para {DocumentId}. " +
            "Hangfire no está configurado - el documento NO será procesado.",
            documentId);
        return Guid.NewGuid().ToString();
    }

    public string EnqueueVectorization(Guid documentId)
    {
        _logger.LogWarning(
            "InMemoryDocumentProcessingQueue: EnqueueVectorization llamado para {DocumentId}. " +
            "Hangfire no está configurado - el documento NO será vectorizado.",
            documentId);
        return Guid.NewGuid().ToString();
    }
}
