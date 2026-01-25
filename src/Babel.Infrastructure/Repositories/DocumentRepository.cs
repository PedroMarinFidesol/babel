using Babel.Application.Interfaces;
using Babel.Domain.Entities;
using Babel.Domain.Enums;
using Babel.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Babel.Infrastructure.Repositories;

/// <summary>
/// Implementación del repositorio de documentos.
/// </summary>
public sealed class DocumentRepository : RepositoryBase<Document>, IDocumentRepository
{
    public DocumentRepository(BabelDbContext context) : base(context)
    {
    }

    public async Task<Document?> GetByIdWithChunksAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(d => d.Chunks.OrderBy(c => c.ChunkIndex))
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> GetByProjectIdAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(d => d.ProjectId == projectId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> GetByStatusAsync(
        DocumentStatus status,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(d => d.Status == status)
            .OrderBy(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> GetByProjectIdAndStatusAsync(
        Guid projectId,
        DocumentStatus status,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(d => d.ProjectId == projectId && d.Status == status)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByHashAsync(
        string contentHash,
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(
                d => d.ContentHash == contentHash && d.ProjectId == projectId,
                cancellationToken);
    }

    public async Task<int> CountByProjectIdAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(d => d.ProjectId == projectId, cancellationToken);
    }

    public async Task<int> CountByStatusAsync(
        Guid projectId,
        DocumentStatus status,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(
                d => d.ProjectId == projectId && d.Status == status,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> GetPendingVectorizationAsync(
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(d => d.Status == DocumentStatus.Completed &&
                        !d.IsVectorized &&
                        !string.IsNullOrEmpty(d.Content))
            .OrderBy(d => d.ProcessedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> GetPendingOcrReviewAsync(
        Guid? projectId = null,
        CancellationToken cancellationToken = default)
    {
        var query = DbSet
            .AsNoTracking()
            .Where(d => d.Status == DocumentStatus.PendingReview && !d.OcrReviewed);

        if (projectId.HasValue)
        {
            query = query.Where(d => d.ProjectId == projectId.Value);
        }

        return await query
            .OrderBy(d => d.ProcessedAt)
            .ToListAsync(cancellationToken);
    }
}
