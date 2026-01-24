using Babel.Domain.Enums;

namespace Babel.Domain.Entities;

public class Document : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;

    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public string? Content { get; set; }

    public bool RequiresOcr { get; set; }
    public bool OcrReviewed { get; set; }

    public DateTime? ProcessedAt { get; set; }
}
