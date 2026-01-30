using Babel.Domain.Entities;
using Babel.Domain.Enums;

namespace Babel.Application.Interfaces;

/// <summary>
/// Repositorio específico para la entidad Document.
/// </summary>
public interface IDocumentRepository : IRepository<Document>
{
    /// <summary>
    /// Obtiene un documento con sus chunks incluidos.
    /// </summary>
    Task<Document?> GetByIdWithChunksAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene todos los documentos de un proyecto.
    /// </summary>
    Task<IReadOnlyList<Document>> GetByProjectIdAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene documentos con un estado específico.
    /// </summary>
    Task<IReadOnlyList<Document>> GetByStatusAsync(
        DocumentStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene documentos de un proyecto con un estado específico.
    /// </summary>
    Task<IReadOnlyList<Document>> GetByProjectIdAndStatusAsync(
        Guid projectId,
        DocumentStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica si existe un documento con el mismo hash en el proyecto.
    /// </summary>
    Task<bool> ExistsByHashAsync(
        string contentHash,
        Guid projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cuenta el total de documentos en un proyecto.
    /// </summary>
    Task<int> CountByProjectIdAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cuenta documentos de un proyecto por estado.
    /// </summary>
    Task<int> CountByStatusAsync(
        Guid projectId,
        DocumentStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene documentos pendientes de vectorización.
    /// </summary>
    Task<IReadOnlyList<Document>> GetPendingVectorizationAsync(
        int limit = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene documentos pendientes de revisión de OCR.
    /// </summary>
    Task<IReadOnlyList<Document>> GetPendingOcrReviewAsync(
        Guid? projectId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca un documento como vectorizado usando update directo (evita conflictos de concurrencia).
    /// </summary>
    Task MarkAsVectorizedAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);
}
