using Babel.Application.Common;
using Babel.Application.Interfaces;
using Babel.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Babel.Infrastructure.Services;

/// <summary>
/// Servicio para operaciones CRUD en Qdrant.
/// </summary>
public class QdrantVectorStoreService : IVectorStoreService
{
    private readonly QdrantClient _qdrantClient;
    private readonly QdrantOptions _options;
    private readonly ILogger<QdrantVectorStoreService> _logger;

    public QdrantVectorStoreService(
        QdrantClient qdrantClient,
        IOptions<QdrantOptions> options,
        ILogger<QdrantVectorStoreService> logger)
    {
        _qdrantClient = qdrantClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result> UpsertChunkAsync(
        Guid pointId,
        ReadOnlyMemory<float> embedding,
        ChunkPayload payload,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var point = CreatePointStruct(pointId, embedding, payload);

            await _qdrantClient.UpsertAsync(
                collectionName: _options.CollectionName,
                points: [point],
                cancellationToken: cancellationToken);

            _logger.LogDebug(
                "Chunk insertado en Qdrant. PointId: {PointId}, DocumentId: {DocumentId}",
                pointId, payload.DocumentId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error insertando chunk en Qdrant. PointId: {PointId}, DocumentId: {DocumentId}",
                pointId, payload.DocumentId);
            return Result.Failure(
                new Error("Vectorization.QdrantOperationFailed", $"Error insertando chunk: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result> UpsertChunksAsync(
        IReadOnlyList<(Guid PointId, ReadOnlyMemory<float> Embedding, ChunkPayload Payload)> chunks,
        CancellationToken cancellationToken = default)
    {
        if (chunks is null || chunks.Count == 0)
        {
            return Result.Success();
        }

        try
        {
            var points = chunks
                .Select(c => CreatePointStruct(c.PointId, c.Embedding, c.Payload))
                .ToList();

            await _qdrantClient.UpsertAsync(
                collectionName: _options.CollectionName,
                points: points,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Insertados {Count} chunks en Qdrant para documento {DocumentId}",
                chunks.Count, chunks.First().Payload.DocumentId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error insertando {Count} chunks en Qdrant", chunks.Count);
            return Result.Failure(
                new Error("Vectorization.QdrantOperationFailed", $"Error insertando chunks: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteByDocumentIdAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Usar filtro por payload para eliminar todos los puntos del documento
            var filter = new Filter
            {
                Must =
                {
                    new Condition
                    {
                        Field = new FieldCondition
                        {
                            Key = "document_id",
                            Match = new Match { Keyword = documentId.ToString() }
                        }
                    }
                }
            };

            await _qdrantClient.DeleteAsync(
                collectionName: _options.CollectionName,
                filter: filter,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Eliminados puntos de Qdrant para DocumentId: {DocumentId}",
                documentId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error eliminando puntos de Qdrant para DocumentId: {DocumentId}",
                documentId);
            return Result.Failure(
                new Error("Vectorization.QdrantOperationFailed", $"Error eliminando puntos: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteByProjectIdAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = new Filter
            {
                Must =
                {
                    new Condition
                    {
                        Field = new FieldCondition
                        {
                            Key = "project_id",
                            Match = new Match { Keyword = projectId.ToString() }
                        }
                    }
                }
            };

            await _qdrantClient.DeleteAsync(
                collectionName: _options.CollectionName,
                filter: filter,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Eliminados puntos de Qdrant para ProjectId: {ProjectId}",
                projectId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error eliminando puntos de Qdrant para ProjectId: {ProjectId}",
                projectId);
            return Result.Failure(
                new Error("Vectorization.QdrantOperationFailed", $"Error eliminando puntos del proyecto: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<VectorSearchResult>>> SearchAsync(
        ReadOnlyMemory<float> queryVector,
        Guid projectId,
        int topK = 5,
        float minScore = 0.7f,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Filtro por project_id para limitar búsqueda al proyecto especificado
            var filter = new Filter
            {
                Must =
                {
                    new Condition
                    {
                        Field = new FieldCondition
                        {
                            Key = "project_id",
                            Match = new Match { Keyword = projectId.ToString() }
                        }
                    }
                }
            };

            var searchResults = await _qdrantClient.SearchAsync(
                collectionName: _options.CollectionName,
                vector: queryVector.ToArray(),
                filter: filter,
                limit: (ulong)topK,
                scoreThreshold: minScore,
                cancellationToken: cancellationToken);

            var results = searchResults
                .Select(point => new VectorSearchResult(
                    PointId: Guid.Parse(point.Id.Uuid),
                    DocumentId: Guid.Parse(point.Payload["document_id"].StringValue),
                    ProjectId: Guid.Parse(point.Payload["project_id"].StringValue),
                    ChunkIndex: (int)point.Payload["chunk_index"].IntegerValue,
                    FileName: point.Payload["file_name"].StringValue,
                    SimilarityScore: point.Score))
                .ToList();

            _logger.LogDebug(
                "Búsqueda vectorial completada. ProjectId: {ProjectId}, Resultados: {Count}",
                projectId, results.Count);

            return Result.Success<IReadOnlyList<VectorSearchResult>>(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error en búsqueda vectorial. ProjectId: {ProjectId}",
                projectId);
            return Result.Failure<IReadOnlyList<VectorSearchResult>>(
                new Error("VectorSearch.Failed", $"Error en búsqueda vectorial: {ex.Message}"));
        }
    }

    /// <summary>
    /// Crea un PointStruct para Qdrant con el embedding y payload.
    /// </summary>
    private static PointStruct CreatePointStruct(
        Guid pointId,
        ReadOnlyMemory<float> embedding,
        ChunkPayload payload)
    {
        return new PointStruct
        {
            Id = new PointId { Uuid = pointId.ToString() },
            Vectors = embedding.ToArray(),
            Payload =
            {
                ["document_id"] = payload.DocumentId.ToString(),
                ["project_id"] = payload.ProjectId.ToString(),
                ["chunk_index"] = payload.ChunkIndex,
                ["file_name"] = payload.FileName
            }
        };
    }
}
