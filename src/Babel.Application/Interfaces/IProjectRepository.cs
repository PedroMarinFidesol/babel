using Babel.Domain.Entities;

namespace Babel.Application.Interfaces;

/// <summary>
/// Repositorio específico para la entidad Project.
/// </summary>
public interface IProjectRepository : IRepository<Project>
{
    /// <summary>
    /// Obtiene un proyecto con sus documentos incluidos.
    /// </summary>
    Task<Project?> GetByIdWithDocumentsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene todos los proyectos con el conteo de documentos.
    /// </summary>
    Task<IReadOnlyList<Project>> GetAllWithDocumentCountAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica si existe un proyecto con el nombre especificado.
    /// </summary>
    Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica si existe un proyecto con el nombre especificado, excluyendo un ID.
    /// Útil para validar en actualizaciones.
    /// </summary>
    Task<bool> ExistsByNameAsync(
        string name,
        Guid excludeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca proyectos por nombre (búsqueda parcial).
    /// </summary>
    Task<IReadOnlyList<Project>> SearchByNameAsync(
        string searchTerm,
        CancellationToken cancellationToken = default);
}
