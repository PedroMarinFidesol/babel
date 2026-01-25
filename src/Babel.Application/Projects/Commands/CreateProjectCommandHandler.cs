using Babel.Application.Common;
using Babel.Application.DTOs;
using Babel.Application.Interfaces;
using Babel.Domain.Entities;

namespace Babel.Application.Projects.Commands;

/// <summary>
/// Handler para CreateProjectCommand.
/// Crea un nuevo proyecto en la base de datos.
/// </summary>
public sealed class CreateProjectCommandHandler : ICommandHandler<CreateProjectCommand, ProjectDto>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProjectCommandHandler(
        IProjectRepository projectRepository,
        IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProjectDto>> Handle(
        CreateProjectCommand request,
        CancellationToken cancellationToken)
    {
        // Verificar si ya existe un proyecto con el mismo nombre
        var existsByName = await _projectRepository.ExistsByNameAsync(request.Name, cancellationToken);
        if (existsByName)
        {
            return Result.Failure<ProjectDto>(DomainErrors.Project.NameAlreadyExists);
        }

        // Crear la entidad Project
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Agregar al repositorio y guardar
        _projectRepository.Add(project);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Mapear a DTO y retornar
        var projectDto = new ProjectDto
        {
            Id = project.Id,
            Name = project.Name,
            Description = project.Description,
            TotalDocuments = 0,
            ProcessedDocuments = 0,
            PendingDocuments = 0,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };

        return Result.Success(projectDto);
    }
}
