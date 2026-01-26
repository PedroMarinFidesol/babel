using Babel.Application.Interfaces;
using Babel.Domain.Entities;
using Babel.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Babel.Infrastructure.Repositories;

/// <summary>
/// Implementación del repositorio de proyectos.
/// </summary>
public sealed class ProjectRepository : RepositoryBase<Project>, IProjectRepository
{
    public ProjectRepository(BabelDbContext context) : base(context)
    {
    }

    public async Task<Project?> GetByIdWithDocumentsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(p => p.Documents)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> GetAllWithDocumentCountAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(p => p.Documents)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(p => p.Name == name, cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(
        string name,
        Guid excludeId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(p => p.Name == name && p.Id != excludeId, cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> SearchByNameAsync(
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(p => p.Documents)
            .AsNoTracking()
            .Where(p => EF.Functions.Like(p.Name, $"%{searchTerm}%"))
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(cancellationToken);
    }
}
