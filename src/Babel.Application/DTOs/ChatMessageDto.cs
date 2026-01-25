namespace Babel.Application.DTOs;

public class ChatMessageDto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Content { get; set; } = string.Empty;
    public bool IsUser { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public List<DocumentReferenceDto> References { get; set; } = [];
}

public class DocumentReferenceDto
{
    public Guid DocumentId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string? Snippet { get; set; }
    public double RelevanceScore { get; set; }
}

public class ChatRequestDto
{
    public Guid ProjectId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ChatResponseDto
{
    public string Response { get; set; } = string.Empty;
    public List<DocumentReferenceDto> References { get; set; } = [];
}
