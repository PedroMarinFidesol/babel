using Babel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Babel.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Project> Projects { get; }
    DbSet<Document> Documents { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
