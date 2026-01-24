using Babel.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Babel.Infrastructure.Data.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.FileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(d => d.FilePath)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(d => d.FileExtension)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.Status)
            .IsRequired()
            .HasConversion<string>(); // Store enum as string

        builder.Property(d => d.Content)
            .HasMaxLength(int.MaxValue); // nvarchar(max)

        builder.Property(d => d.RequiresOcr)
            .IsRequired();

        builder.Property(d => d.OcrReviewed)
            .IsRequired();

        builder.HasOne(d => d.Project)
            .WithMany(p => p.Documents)
            .HasForeignKey(d => d.ProjectId);
    }
}
