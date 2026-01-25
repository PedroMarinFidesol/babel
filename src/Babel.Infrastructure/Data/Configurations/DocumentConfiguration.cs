using Babel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Babel.Infrastructure.Data.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.HasKey(d => d.Id);

        // Índice para búsquedas por proyecto
        builder.HasIndex(d => d.ProjectId);

        // Índice para búsquedas por hash (deduplicación)
        builder.HasIndex(d => d.ContentHash);

        // Índice para búsquedas por estado
        builder.HasIndex(d => d.Status);

        #region Información del Archivo Físico

        builder.Property(d => d.FileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(d => d.FileExtension)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.FilePath)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(d => d.FileSizeBytes)
            .IsRequired();

        builder.Property(d => d.ContentHash)
            .IsRequired()
            .HasMaxLength(64); // SHA256 = 64 caracteres hex

        builder.Property(d => d.MimeType)
            .IsRequired()
            .HasMaxLength(100);

        #endregion

        #region Clasificación

        builder.Property(d => d.FileType)
            .IsRequired()
            .HasConversion<string>(); // Almacenar enum como string

        #endregion

        #region Estado de Procesamiento

        builder.Property(d => d.Status)
            .IsRequired()
            .HasConversion<string>(); // Almacenar enum como string

        builder.Property(d => d.RequiresOcr)
            .IsRequired();

        builder.Property(d => d.OcrReviewed)
            .IsRequired();

        builder.Property(d => d.ProcessedAt);

        #endregion

        #region Contenido Extraído

        builder.Property(d => d.Content)
            .HasMaxLength(int.MaxValue); // nvarchar(max)

        #endregion

        #region Vectorización

        builder.Property(d => d.IsVectorized)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(d => d.VectorizedAt);

        #endregion

        #region Relaciones

        builder.HasOne(d => d.Project)
            .WithMany(p => p.Documents)
            .HasForeignKey(d => d.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.Chunks)
            .WithOne(c => c.Document)
            .HasForeignKey(c => c.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        #endregion
    }
}
