using Babel.Application.Common;
using Babel.Application.DTOs;
using Babel.Application.Interfaces;
using Babel.Domain.Enums;

namespace Babel.Application.Projects.Commands;

/// <summary>
/// Handler para UpdateProjectCommand.
/// </summary>
public sealed class UpdateProjectCommandHandler : ICommandHandler<UpdateProjectCommand, ProjectDto>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProjectCommandHandler(
        IProjectRepository projectRepository,
        IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProjectDto>> Handle(
        UpdateProjectCommand request,
        CancellationToken cancellationToken)
    {
        // Obtener el proyecto existente
        var project = await _projectRepository.GetByIdWithDocumentsAsync(request.Id, cancellationToken);

        if (project is null)
        {
            return Result.Failure<ProjectDto>(DomainErrors.Project.NotFound);
        }

        // Verificar si el nuevo nombre ya existe (excluyendo el proyecto actual)
        if (!string.Equals(project.Name, request.Name, StringComparison.OrdinalIgnoreCase))
        {
            var nameExists = await _projectRepository.ExistsByNameAsync(request.Name, request.Id, cancellationToken);
            if (nameExists)
            {
                return Result.Failure<ProjectDto>(DomainErrors.Project.NameAlreadyExists);
            }
        }

        // Actualizar campos
        project.Name = request.Name.Trim();
        project.Description = request.Description?.Trim();
        project.UpdatedAt = DateTime.UtcNow;

        // Guardar cambios
        _projectRepository.Update(project);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Mapear a DTO
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
