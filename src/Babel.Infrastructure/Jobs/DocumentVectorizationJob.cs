using Babel.Application.Interfaces;
using Babel.Domain.Entities;
using Babel.Domain.Enums;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Babel.Infrastructure.Jobs;

/// <summary>
/// Job de Hangfire para vectorizar documentos.
/// Divide el contenido en chunks, genera embeddings y los almacena en Qdrant.
/// </summary>
public class DocumentVectorizationJob
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IChunkingService _chunkingService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStoreService _vectorStoreService;
    private readonly ILogger<DocumentVectorizationJob> _logger;

    public DocumentVectorizationJob(
        IDocumentRepository documentRepository,
        IUnitOfWork unitOfWork,
        IChunkingService chunkingService,
        IEmbeddingService embeddingService,
        IVectorStoreService vectorStoreService,
        ILogger<DocumentVectorizationJob> logger)
    {
        _documentRepository = documentRepository;
        _unitOfWork = unitOfWork;
        _chunkingService = chunkingService;
        _embeddingService = embeddingService;
        _vectorStoreService = vectorStoreService;
        _logger = logger;
    }

    /// <summary>
    /// Vectoriza un documento: divide en chunks, genera embeddings y almacena en Qdrant.
    /// </summary>
    /// <param name="documentId">ID del documento a vectorizar</param>
    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    [JobDisplayName("Vectorizar documento: {0}")]
    public async Task ProcessAsync(Guid documentId)
    {
        _logger.LogInformation("Iniciando vectorización para documento {DocumentId}", documentId);

        // 1. Obtener documento con chunks existentes
        var document = await _documentRepository.GetByIdWithChunksAsync(documentId);

        if (document is null)
        {
            _logger.LogWarning("Documento {DocumentId} no encontrado para vectorización", documentId);
            throw new InvalidOperationException($"Documento {documentId} no encontrado");
        }

        // 2. Validar que el documento está listo para vectorización
        if (string.IsNullOrWhiteSpace(document.Content))
        {
            _logger.LogWarning(
                "Documento {DocumentId} no tiene contenido para vectorizar",
                documentId);
            throw new InvalidOperationException("El documento no tiene contenido para vectorizar");
        }

        if (document.Status != DocumentStatus.Completed)
        {
            _logger.LogWarning(
                "Documento {DocumentId} no está en estado Completed. Estado actual: {Status}",
                documentId, document.Status);
            throw new InvalidOperationException($"El documento debe estar en estado Completed, estado actual: {document.Status}");
        }

        // 3. Si ya estaba vectorizado, eliminar chunks existentes (re-vectorización)
        if (document.IsVectorized || document.Chunks.Count > 0)
        {
            _logger.LogInformation(
                "Re-vectorizando documento {DocumentId}. Eliminando {ChunkCount} chunks existentes",
                documentId, document.Chunks.Count);

            // Eliminar de Qdrant
            var deleteResult = await _vectorStoreService.DeleteByDocumentIdAsync(documentId);
            if (deleteResult.IsFailure)
            {
                _logger.LogWarning(
                    "Error eliminando puntos de Qdrant para documento {DocumentId}: {Error}",
                    documentId, deleteResult.Error.Description);
            }

            // Eliminar chunks de la BD
            document.Chunks.Clear();
            document.IsVectorized = false;
            document.VectorizedAt = null;
        }

        // 4. Dividir contenido en chunks
        var chunkResults = _chunkingService.ChunkText(document.Content, documentId);

        if (chunkResults.Count == 0)
        {
            _logger.LogWarning(
                "Chunking no produjo resultados para documento {DocumentId}",
                documentId);
            throw new InvalidOperationException("El chunking no produjo resultados");
        }

        _logger.LogInformation(
            "Documento {DocumentId} dividido en {ChunkCount} chunks",
            documentId, chunkResults.Count);

        // 5. Generar embeddings en batch
        var textsToEmbed = chunkResults.Select(c => c.Content).ToList();
        var embeddingsResult = await _embeddingService.GenerateEmbeddingsAsync(textsToEmbed);

        if (embeddingsResult.IsFailure)
        {
            _logger.LogError(
                "Error generando embeddings para documento {DocumentId}: {Error}",
                documentId, embeddingsResult.Error.Description);
            throw new InvalidOperationException($"Error generando embeddings: {embeddingsResult.Error.Description}");
        }

        var embeddings = embeddingsResult.Value;

        // 6. Crear DocumentChunks y preparar datos para Qdrant
        var chunksForQdrant = new List<(Guid PointId, ReadOnlyMemory<float> Embedding, ChunkPayload Payload)>();

        for (int i = 0; i < chunkResults.Count; i++)
        {
            var chunkResult = chunkResults[i];
            var embedding = embeddings[i];
            var pointId = Guid.NewGuid();

            // Crear entidad DocumentChunk
            var documentChunk = new DocumentChunk
            {
                Id = Guid.NewGuid(),
                DocumentId = documentId,
                ChunkIndex = chunkResult.ChunkIndex,
                StartCharIndex = chunkResult.StartCharIndex,
                EndCharIndex = chunkResult.EndCharIndex,
                Content = chunkResult.Content,
                TokenCount = chunkResult.EstimatedTokenCount,
                QdrantPointId = pointId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            document.Chunks.Add(documentChunk);

            // Preparar para Qdrant
            var payload = new ChunkPayload(
                DocumentId: documentId,
                ProjectId: document.ProjectId,
                ChunkIndex: chunkResult.ChunkIndex,
                FileName: document.FileName);

            chunksForQdrant.Add((pointId, embedding, payload));
        }

        // 7. Marcar documento como vectorizado (en memoria, antes de guardar)
        document.IsVectorized = true;
        document.VectorizedAt = DateTime.UtcNow;

        // 8. Guardar en Qdrant PRIMERO (si falla, no guardamos en BD)
        var upsertResult = await _vectorStoreService.UpsertChunksAsync(chunksForQdrant);

        if (upsertResult.IsFailure)
        {
            _logger.LogError(
                "Error insertando chunks en Qdrant para documento {DocumentId}: {Error}",
                documentId, upsertResult.Error.Description);
            throw new InvalidOperationException($"Error guardando en Qdrant: {upsertResult.Error.Description}");
        }

        // 9. Guardar TODO en SQL Server (documento + chunks en una sola transacción)
        await _unitOfWork.SaveChangesAsync();

        _logger.LogDebug(
            "Documento y chunks guardados en SQL Server para documento {DocumentId}",
            documentId);

        _logger.LogInformation(
            "Documento {DocumentId} vectorizado exitosamente. Chunks: {ChunkCount}, Dimensiones: {Dimensions}",
            documentId, chunkResults.Count, _embeddingService.GetVectorDimension());
    }
}
