using Babel.Application.Common;
using Babel.Application.DTOs;
using Babel.Application.Interfaces;

namespace Babel.Application.Documents.Queries;

/// <summary>
/// Handler para GetDocumentsByProjectQuery.
/// </summary>
public sealed class GetDocumentsByProjectQueryHandler : IQueryHandler<GetDocumentsByProjectQuery, List<DocumentDto>>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IProjectRepository _projectRepository;

    public GetDocumentsByProjectQueryHandler(
        IDocumentRepository documentRepository,
        IProjectRepository projectRepository)
    {
        _documentRepository = documentRepository;
        _projectRepository = projectRepository;
    }

    public async Task<Result<List<DocumentDto>>> Handle(
        GetDocumentsByProjectQuery request,
        CancellationToken cancellationToken)
    {
        // Verificar que el proyecto existe
        var projectExists = await _projectRepository.ExistsAsync(request.ProjectId, cancellationToken);
        if (!projectExists)
        {
            return Result.Failure<List<DocumentDto>>(DomainErrors.Document.ProjectNotFound);
        }

        var documents = await _documentRepository.GetByProjectIdAsync(request.ProjectId, cancellationToken);

        var documentDtos = documents.Select(d => new DocumentDto
        {
            Id = d.Id,
            ProjectId = d.ProjectId,
            FileName = d.FileName,
            FileExtension = d.FileExtension,
            FileSizeBytes = d.FileSizeBytes,
            MimeType = d.MimeType,
            Status = d.Status,
            IsVectorized = d.IsVectorized,
            CreatedAt = d.CreatedAt,
            ProcessedAt = d.ProcessedAt
        })
        .OrderByDescending(d => d.CreatedAt)
        .ToList();

        return Result.Success(documentDtos);
    }
}
