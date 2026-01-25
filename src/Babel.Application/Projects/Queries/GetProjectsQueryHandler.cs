using Babel.Application.Common;
using Babel.Application.DTOs;
using Babel.Application.Interfaces;
using Babel.Domain.Enums;

namespace Babel.Application.Projects.Queries;

/// <summary>
/// Handler para GetProjectsQuery.
/// Obtiene todos los proyectos con sus conteos de documentos.
/// </summary>
public sealed class GetProjectsQueryHandler : IQueryHandler<GetProjectsQuery, List<ProjectDto>>
{
    private readonly IProjectRepository _projectRepository;

    public GetProjectsQueryHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<Result<List<ProjectDto>>> Handle(
        GetProjectsQuery request,
        CancellationToken cancellationToken)
    {
        var projects = await _projectRepository.GetAllWithDocumentCountAsync(cancellationToken);

        var projectDtos = projects.Select(p => new ProjectDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            TotalDocuments = p.Documents.Count,
            ProcessedDocuments = p.Documents.Count(d => d.Status == DocumentStatus.Completed),
            PendingDocuments = p.Documents.Count(d =>
                d.Status == DocumentStatus.Pending ||
                d.Status == DocumentStatus.Processing ||
                d.Status == DocumentStatus.PendingReview),
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        })
        .OrderByDescending(p => p.UpdatedAt)
        .ToList();

        return Result.Success(projectDtos);
    }
}
