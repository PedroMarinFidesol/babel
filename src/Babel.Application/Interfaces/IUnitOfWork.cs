namespace Babel.Application.Interfaces;

/// <summary>
/// Unit of Work para coordinar transacciones entre repositorios.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Repositorio de proyectos.
    /// </summary>
    IProjectRepository Projects { get; }

    /// <summary>
    /// Repositorio de documentos.
    /// </summary>
    IDocumentRepository Documents { get; }

    /// <summary>
    /// Guarda todos los cambios pendientes en la base de datos.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Inicia una transacción de base de datos.
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirma la transacción actual.
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Revierte la transacción actual.
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
