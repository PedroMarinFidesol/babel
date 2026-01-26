using Babel.Application.Common;
using Babel.Application.DTOs;
using Babel.Application.Interfaces;

namespace Babel.Application.Documents.Queries;

/// <summary>
/// Handler para GetDocumentByIdQuery.
/// </summary>
public sealed class GetDocumentByIdQueryHandler : IQueryHandler<GetDocumentByIdQuery, DocumentDto>
{
    private readonly IDocumentRepository _documentRepository;

    public GetDocumentByIdQueryHandler(IDocumentRepository documentRepository)
    {
        _documentRepository = documentRepository;
    }

    public async Task<Result<DocumentDto>> Handle(
        GetDocumentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(request.Id, cancellationToken);

        if (document is null)
        {
            return Result.Failure<DocumentDto>(DomainErrors.Document.NotFound);
        }

        var documentDto = new DocumentDto
        {
            Id = document.Id,
            ProjectId = document.ProjectId,
            FileName = document.FileName,
            FileExtension = document.FileExtension,
            FileSizeBytes = document.FileSizeBytes,
            MimeType = document.MimeType,
            Status = document.Status,
            IsVectorized = document.IsVectorized,
            CreatedAt = document.CreatedAt,
            ProcessedAt = document.ProcessedAt
        };

        return Result.Success(documentDto);
    }
}
