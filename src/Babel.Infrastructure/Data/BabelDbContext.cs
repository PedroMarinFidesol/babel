using Babel.Application.Interfaces;
using Babel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Babel.Infrastructure.Data;

public class BabelDbContext : DbContext, IApplicationDbContext
{
    public BabelDbContext(DbContextOptions<BabelDbContext> options) : base(options)
    {
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Document> Documents => Set<Document>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations from current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update timestamps
        var entries = ChangeTracker.Entries<BaseEntity>();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
