using Babel.Application.Common;
using Babel.Application.DTOs;
using Babel.Application.Interfaces;
using Babel.Domain.Enums;

namespace Babel.Application.Projects.Queries;

/// <summary>
/// Handler para GetProjectByIdQuery.
/// </summary>
public sealed class GetProjectByIdQueryHandler : IQueryHandler<GetProjectByIdQuery, ProjectDto>
{
    private readonly IProjectRepository _projectRepository;

    public GetProjectByIdQueryHandler(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<Result<ProjectDto>> Handle(
        GetProjectByIdQuery request,
        CancellationToken cancellationToken)
    {
        var project = await _projectRepository.GetByIdWithDocumentsAsync(request.Id, cancellationToken);

        if (project is null)
        {
            return Result.Failure<ProjectDto>(DomainErrors.Project.NotFound);
        }

        var projectDto = new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            TotalDocuments = project.Documents.Count,
            ProcessedDocuments = project.Documents.Count(d => d.Status == DocumentStatus.Completed),
            PendingDocuments = project.Documents.Count(d =>
                d.Status == DocumentStatus.Pending ||
                d.Status == DocumentStatus.Processing ||
                d.Status == DocumentStatus.PendingReview),
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };

        return Result.Success(projectDto);
    }
}
