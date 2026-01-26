using Babel.Application.Interfaces;
using Babel.Infrastructure.Jobs;
using Hangfire;

namespace Babel.Infrastructure.Services;

/// <summary>
/// Implementación de IDocumentProcessingQueue usando Hangfire.
/// </summary>
public class DocumentProcessingQueue : IDocumentProcessingQueue
{
    private readonly IBackgroundJobClient _backgroundJobClient;

    public DocumentProcessingQueue(IBackgroundJobClient backgroundJobClient)
    {
        _backgroundJobClient = backgroundJobClient;
    }

    public string EnqueueTextExtraction(Guid documentId)
    {
        return _backgroundJobClient.Enqueue<DocumentProcessingJob>(
            job => job.ProcessAsync(documentId));
    }

    public string EnqueueOcrProcessing(Guid documentId)
    {
        // TODO: Implementar job de OCR
        // Por ahora, encolar el mismo job de procesamiento
        return _backgroundJobClient.Enqueue<DocumentProcessingJob>(
            job => job.ProcessAsync(documentId));
    }

    public string EnqueueVectorization(Guid documentId)
    {
        return _backgroundJobClient.Enqueue<DocumentVectorizationJob>(
            job => job.ProcessAsync(documentId));
    }
}
