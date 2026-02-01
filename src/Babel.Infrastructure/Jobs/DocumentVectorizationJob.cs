using Babel.Application.Interfaces;
using Babel.Domain.Entities;
using Babel.Domain.Enums;
using Babel.Infrastructure.Data;
using Hangfire;
using Microsoft.EntityFrameworkCore;
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
    private readonly BabelDbContext _dbContext;

    public DocumentVectorizationJob(
        IDocumentRepository documentRepository,
        IUnitOfWork unitOfWork,
        IChunkingService chunkingService,
        IEmbeddingService embeddingService,
        IVectorStoreService vectorStoreService,
        ILogger<DocumentVectorizationJob> logger,
        BabelDbContext dbContext)
    {
        _documentRepository = documentRepository;
        _unitOfWork = unitOfWork;
        _chunkingService = chunkingService;
        _embeddingService = embeddingService;
        _vectorStoreService = vectorStoreService;
        _logger = logger;
        _dbContext = dbContext;
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

        // =========================================================================================
        // FASE 1: LEER DATOS (READ-ONLY)
        // =========================================================================================
        
        var initialDoc = await _documentRepository.GetByIdWithChunksAsync(documentId);

        if (initialDoc is null)
        {
            _logger.LogWarning("Documento {DocumentId} no encontrado para vectorización", documentId);
            throw new InvalidOperationException($"Documento {documentId} no encontrado");
        }

        if (string.IsNullOrWhiteSpace(initialDoc.Content))
        {
            _logger.LogWarning("Documento {DocumentId} no tiene contenido", documentId);
            throw new InvalidOperationException("El documento no tiene contenido para vectorizar");
        }

        if (initialDoc.Status != DocumentStatus.Completed)
        {
            _logger.LogWarning("Documento {DocumentId} no está en estado Completed", documentId);
            throw new InvalidOperationException($"Estado inválido: {initialDoc.Status}");
        }

        string content = initialDoc.Content;
        string fileName = initialDoc.FileName;
        Guid projectId = initialDoc.ProjectId;
        
        // Liberamos contexto
        initialDoc = null; 

        // =========================================================================================
        // FASE 2: PROCESAMIENTO PESADO (SIN BD)
        // =========================================================================================

        // Chunking
        var chunkResults = _chunkingService.ChunkText(content, documentId);

        if (chunkResults.Count == 0)
        {
            _logger.LogWarning("Chunking no produjo resultados para {DocumentId}", documentId);
            throw new InvalidOperationException("El chunking no produjo resultados");
        }

        _logger.LogInformation("Documento {DocumentId}: {ChunkCount} chunks generados", documentId, chunkResults.Count);

        // Embeddings
        var textsToEmbed = chunkResults.Select(c => c.Content).ToList();
        var embeddingsResult = await _embeddingService.GenerateEmbeddingsAsync(textsToEmbed);

        if (embeddingsResult.IsFailure)
        {
            _logger.LogError("Error generando embeddings: {Error}", embeddingsResult.Error.Description);
            throw new InvalidOperationException($"Error embeddings: {embeddingsResult.Error.Description}");
        }

        var embeddings = embeddingsResult.Value;

        // Preparar datos
        var newChunkEntities = new List<DocumentChunk>();
        var chunksForQdrant = new List<(Guid PointId, ReadOnlyMemory<float> Embedding, ChunkPayload Payload)>();

        for (int i = 0; i < chunkResults.Count; i++)
        {
            var chunkResult = chunkResults[i];
            var embedding = embeddings[i];
            var pointId = Guid.NewGuid();

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
            newChunkEntities.Add(documentChunk);

            var payload = new ChunkPayload(
                DocumentId: documentId,
                ProjectId: projectId,
                ChunkIndex: chunkResult.ChunkIndex,
                FileName: fileName);

            chunksForQdrant.Add((pointId, embedding, payload));
        }

        // Qdrant Upsert (Idempotente)
        await _vectorStoreService.DeleteByDocumentIdAsync(documentId);
        
        var upsertResult = await _vectorStoreService.UpsertChunksAsync(chunksForQdrant);
        if (upsertResult.IsFailure)
        {
             _logger.LogError("Error insertando en Qdrant: {Error}", upsertResult.Error.Description);
            throw new InvalidOperationException($"Error Qdrant: {upsertResult.Error.Description}");
        }

        // =========================================================================================
        // FASE 3: PERSISTENCIA (COMMAND-BASED + TRANSACTION)
        // Usamos comandos directos para evitar conflictos de concurrencia y tracking de EF.
        // =========================================================================================

        using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try 
        {
            // 1. Eliminar chunks anteriores (si existen) directamente
            await _dbContext.DocumentChunks
                .Where(c => c.DocumentId == documentId)
                .ExecuteDeleteAsync();

            // 2. Insertar nuevos chunks (EF Tracking solo para nuevos objetos)
            await _dbContext.DocumentChunks.AddRangeAsync(newChunkEntities);
            await _dbContext.SaveChangesAsync();

            // 3. Actualizar documento directamente
            await _dbContext.Documents
                .Where(d => d.Id == documentId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(d => d.IsVectorized, true)
                    .SetProperty(d => d.VectorizedAt, DateTime.UtcNow)
                    .SetProperty(d => d.UpdatedAt, DateTime.UtcNow));

            await transaction.CommitAsync();

            _logger.LogInformation(
                "Documento {DocumentId} vectorizado exitosamente. Chunks en SQL: {ChunkCount}", 
                documentId, newChunkEntities.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en transacción SQL para documento {DocumentId}", documentId);
            await transaction.RollbackAsync();
            throw;
        }
    }
}
