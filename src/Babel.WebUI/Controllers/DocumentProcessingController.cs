using Babel.Application.Interfaces;
using Babel.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Babel.WebUI.Controllers;

/// <summary>
/// Controlador para procesamiento manual de documentos.
/// Útil cuando Hangfire no está configurado o para reprocesar documentos.
/// </summary>
[ApiController]
[Route("api/documents")]
public class DocumentProcessingController : ControllerBase
{
    private readonly ITextExtractionService _textExtractionService;
    private readonly IDocumentRepository _documentRepository;
    private readonly IChunkingService _chunkingService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStoreService _vectorStoreService;
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<DocumentProcessingController> _logger;

    public DocumentProcessingController(
        ITextExtractionService textExtractionService,
        IDocumentRepository documentRepository,
        IChunkingService chunkingService,
        IEmbeddingService embeddingService,
        IVectorStoreService vectorStoreService,
        IApplicationDbContext dbContext,
        ILogger<DocumentProcessingController> logger)
    {
        _textExtractionService = textExtractionService;
        _documentRepository = documentRepository;
        _chunkingService = chunkingService;
        _embeddingService = embeddingService;
        _vectorStoreService = vectorStoreService;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Procesa y vectoriza un documento específico.
    /// </summary>
    [HttpPost("{documentId:guid}/process")]
    public async Task<IActionResult> ProcessDocument(Guid documentId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Procesamiento manual iniciado para documento {DocumentId}", documentId);

        // 1. Extraer texto si no tiene contenido
        var extractResult = await _textExtractionService.ExtractTextAsync(documentId, cancellationToken);
        if (extractResult.IsFailure && extractResult.Error.Code != "Document.AlreadyProcessed")
        {
            return BadRequest(new { error = extractResult.Error.Description });
        }

        // 2. Vectorizar
        var vectorizeResult = await VectorizeDocumentAsync(documentId, cancellationToken);
        if (!vectorizeResult.Success)
        {
            return BadRequest(new { error = vectorizeResult.Error });
        }

        return Ok(new { message = $"Documento procesado y vectorizado. Chunks: {vectorizeResult.ChunkCount}" });
    }

    /// <summary>
    /// Procesa y vectoriza todos los documentos pendientes de un proyecto.
    /// </summary>
    [HttpPost("project/{projectId:guid}/process-all")]
    public async Task<IActionResult> ProcessAllDocuments(Guid projectId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Procesamiento masivo iniciado para proyecto {ProjectId}", projectId);

        var documents = await _dbContext.Documents
            .Where(d => d.ProjectId == projectId && !d.IsVectorized)
            .ToListAsync(cancellationToken);

        if (documents.Count == 0)
        {
            return Ok(new { message = "No hay documentos pendientes de vectorizar. Usa /reprocess-all para forzar re-vectorización." });
        }

        return await ProcessDocumentsInternal(documents, cancellationToken);
    }

    /// <summary>
    /// FUERZA re-vectorización de TODOS los documentos de un proyecto (incluso los ya vectorizados).
    /// </summary>
    [HttpPost("project/{projectId:guid}/reprocess-all")]
    public async Task<IActionResult> ReprocessAllDocuments(Guid projectId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Re-vectorización forzada para proyecto {ProjectId}", projectId);

        var documents = await _dbContext.Documents
            .Where(d => d.ProjectId == projectId)
            .ToListAsync(cancellationToken);

        if (documents.Count == 0)
        {
            return Ok(new { message = "No hay documentos en este proyecto" });
        }

        return await ProcessDocumentsInternal(documents, cancellationToken);
    }

    private async Task<IActionResult> ProcessDocumentsInternal(List<Babel.Domain.Entities.Document> documents, CancellationToken cancellationToken)
    {
        var results = new List<object>();

        foreach (var doc in documents)
        {
            try
            {
                // Extraer texto si no tiene
                if (string.IsNullOrWhiteSpace(doc.Content))
                {
                    var extractResult = await _textExtractionService.ExtractTextAsync(doc.Id, cancellationToken);
                    if (extractResult.IsFailure && extractResult.Error.Code != "Document.AlreadyProcessed")
                    {
                        results.Add(new { documentId = doc.Id, fileName = doc.FileName, error = extractResult.Error.Description });
                        continue;
                    }
                }

                // Vectorizar
                var vectorizeResult = await VectorizeDocumentAsync(doc.Id, cancellationToken);
                results.Add(new
                {
                    documentId = doc.Id,
                    fileName = doc.FileName,
                    success = vectorizeResult.Success,
                    chunks = vectorizeResult.ChunkCount,
                    error = vectorizeResult.Error
                });
            }
            catch (Exception ex)
            {
                results.Add(new { documentId = doc.Id, fileName = doc.FileName, error = ex.Message });
            }
        }

        return Ok(new { processed = results.Count, results });
    }

    private async Task<(bool Success, int ChunkCount, string? Error)> VectorizeDocumentAsync(
        Guid documentId, CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdWithChunksAsync(documentId, cancellationToken);

        if (document is null)
            return (false, 0, "Documento no encontrado");

        if (string.IsNullOrWhiteSpace(document.Content))
            return (false, 0, "El documento no tiene contenido");

        // Limpiar vectores existentes
        if (document.IsVectorized || document.Chunks.Count > 0)
        {
            await _vectorStoreService.DeleteByDocumentIdAsync(documentId, cancellationToken);
            document.Chunks.Clear();
        }

        // Dividir en chunks
        var chunkResults = _chunkingService.ChunkText(document.Content, documentId);
        if (chunkResults.Count == 0)
            return (false, 0, "El chunking no produjo resultados");

        // Generar embeddings
        var textsToEmbed = chunkResults.Select(c => c.Content).ToList();
        var embeddingsResult = await _embeddingService.GenerateEmbeddingsAsync(textsToEmbed, cancellationToken);

        if (embeddingsResult.IsFailure)
            return (false, 0, $"Error generando embeddings: {embeddingsResult.Error.Description}");

        var embeddings = embeddingsResult.Value;

        // Crear chunks y preparar para Qdrant
        var chunksForQdrant = new List<(Guid PointId, ReadOnlyMemory<float> Embedding, ChunkPayload Payload)>();

        for (int i = 0; i < chunkResults.Count; i++)
        {
            var chunkResult = chunkResults[i];
            var embedding = embeddings[i];
            var pointId = Guid.NewGuid();

            var documentChunk = new Babel.Domain.Entities.DocumentChunk
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

            var payload = new ChunkPayload(
                DocumentId: documentId,
                ProjectId: document.ProjectId,
                ChunkIndex: chunkResult.ChunkIndex,
                FileName: document.FileName);

            chunksForQdrant.Add((pointId, embedding, payload));
        }

        // Marcar como vectorizado (en memoria, antes de guardar)
        document.IsVectorized = true;
        document.VectorizedAt = DateTime.UtcNow;

        // Guardar en Qdrant PRIMERO (si falla, no guardamos en BD)
        var upsertResult = await _vectorStoreService.UpsertChunksAsync(chunksForQdrant, cancellationToken);
        if (upsertResult.IsFailure)
            return (false, 0, $"Error guardando en Qdrant: {upsertResult.Error.Description}");

        // Guardar TODO en SQL Server (documento + chunks en una sola transacción)
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Documento {DocumentId} vectorizado manualmente. Chunks: {ChunkCount}",
            documentId, chunkResults.Count);

        return (true, chunkResults.Count, null);
    }
}
