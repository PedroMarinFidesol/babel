using Babel.Application.Interfaces;
using Babel.Domain.Entities;
using Babel.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Babel.Infrastructure.Repositories;

/// <summary>
/// Implementación base de repositorio genérico usando Entity Framework Core.
/// </summary>
/// <typeparam name="T">Tipo de entidad que hereda de BaseEntity.</typeparam>
public abstract class RepositoryBase<T> : IRepository<T> where T : BaseEntity
{
    protected readonly BabelDbContext Context;
    protected readonly DbSet<T> DbSet;

    protected RepositoryBase(BabelDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync([id], cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }

    public virtual async Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await DbSet.AnyAsync(e => e.Id == id, cancellationToken);
    }

    public virtual void Add(T entity)
    {
        DbSet.Add(entity);
    }

    public virtual void Update(T entity)
    {
        DbSet.Update(entity);
    }

    public virtual void Remove(T entity)
    {
        DbSet.Remove(entity);
    }
}
