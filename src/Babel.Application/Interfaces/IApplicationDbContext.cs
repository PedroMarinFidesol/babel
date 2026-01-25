using Babel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Babel.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Project> Projects { get; }
    DbSet<Document> Documents { get; }
    DbSet<DocumentChunk> DocumentChunks { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
