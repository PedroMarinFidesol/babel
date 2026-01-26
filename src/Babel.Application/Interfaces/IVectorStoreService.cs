using Babel.Application.Common;

namespace Babel.Application.Interfaces;

/// <summary>
/// Payload con metadatos de un chunk almacenado en Qdrant.
/// </summary>
/// <param name="DocumentId">ID del documento en SQL Server</param>
/// <param name="ProjectId">ID del proyecto para filtrado</param>
/// <param name="ChunkIndex">Índice del chunk dentro del documento</param>
/// <param name="FileName">Nombre del archivo original</param>
public record ChunkPayload(
    Guid DocumentId,
    Guid ProjectId,
    int ChunkIndex,
    string FileName);

/// <summary>
/// Servicio para operaciones CRUD en la base de datos vectorial.
/// Abstracción sobre Qdrant.
/// </summary>
public interface IVectorStoreService
{
    /// <summary>
    /// Inserta o actualiza un chunk en Qdrant.
    /// </summary>
    /// <param name="pointId">ID único del punto en Qdrant</param>
    /// <param name="embedding">Vector de embedding</param>
    /// <param name="payload">Metadatos del chunk</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    Task<Result> UpsertChunkAsync(
        Guid pointId,
        ReadOnlyMemory<float> embedding,
        ChunkPayload payload,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserta o actualiza múltiples chunks en Qdrant (batch).
    /// Más eficiente para vectorización masiva.
    /// </summary>
    /// <param name="chunks">Lista de chunks con sus embeddings y payloads</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    Task<Result> UpsertChunksAsync(
        IReadOnlyList<(Guid PointId, ReadOnlyMemory<float> Embedding, ChunkPayload Payload)> chunks,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina todos los puntos asociados a un documento.
    /// </summary>
    /// <param name="documentId">ID del documento</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    Task<Result> DeleteByDocumentIdAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Elimina todos los puntos asociados a un proyecto.
    /// </summary>
    /// <param name="projectId">ID del proyecto</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    Task<Result> DeleteByProjectIdAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);
}
