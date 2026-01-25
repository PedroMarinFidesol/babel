using Babel.Application.Interfaces;
using Babel.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace Babel.Infrastructure.Repositories;

/// <summary>
/// Implementación de Unit of Work para coordinar transacciones.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly BabelDbContext _context;
    private IDbContextTransaction? _transaction;

    private IProjectRepository? _projects;
    private IDocumentRepository? _documents;

    public UnitOfWork(BabelDbContext context)
    {
        _context = context;
    }

    public IProjectRepository Projects =>
        _projects ??= new ProjectRepository(_context);

    public IDocumentRepository Documents =>
        _documents ??= new DocumentRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
    }
}
