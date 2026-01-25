using Babel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Babel.Infrastructure.Data.Configurations;

public class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.HasKey(c => c.Id);

        // Índice compuesto para búsquedas por documento y orden de chunk
        builder.HasIndex(c => new { c.DocumentId, c.ChunkIndex })
            .IsUnique();

        // Índice para búsquedas por QdrantPointId
        builder.HasIndex(c => c.QdrantPointId);

        // Posición del chunk
        builder.Property(c => c.ChunkIndex)
            .IsRequired();

        builder.Property(c => c.StartCharIndex)
            .IsRequired();

        builder.Property(c => c.EndCharIndex)
            .IsRequired();

        // Contenido del chunk
        builder.Property(c => c.Content)
            .IsRequired()
            .HasMaxLength(int.MaxValue); // nvarchar(max)

        builder.Property(c => c.TokenCount)
            .IsRequired();

        // Referencia a Qdrant
        builder.Property(c => c.QdrantPointId)
            .IsRequired();

        // Metadatos opcionales
        builder.Property(c => c.PageNumber)
            .HasMaxLength(50);

        builder.Property(c => c.SectionTitle)
            .HasMaxLength(500);

        // Relación con Document
        builder.HasOne(c => c.Document)
            .WithMany(d => d.Chunks)
            .HasForeignKey(c => c.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
