using Babel.Domain.Entities;
using System.Linq.Expressions;

namespace Babel.Application.Interfaces;

/// <summary>
/// Repositorio genérico base para operaciones CRUD.
/// </summary>
/// <typeparam name="T">Tipo de entidad que hereda de BaseEntity.</typeparam>
public interface IRepository<T> where T : BaseEntity
{
    /// <summary>
    /// Obtiene una entidad por su ID.
    /// </summary>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene todas las entidades.
    /// </summary>
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca entidades que cumplan un predicado.
    /// </summary>
    Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica si existe una entidad con el ID especificado.
    /// </summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Agrega una nueva entidad al contexto.
    /// </summary>
    void Add(T entity);

    /// <summary>
    /// Marca una entidad como modificada.
    /// </summary>
    void Update(T entity);

    /// <summary>
    /// Marca una entidad para eliminación.
    /// </summary>
    void Remove(T entity);
}
