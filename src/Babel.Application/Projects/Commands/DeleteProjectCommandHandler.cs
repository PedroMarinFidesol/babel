using Babel.Application.Common;
using Babel.Application.Interfaces;

namespace Babel.Application.Projects.Commands;

/// <summary>
/// Handler para DeleteProjectCommand.
/// Elimina un proyecto y todos sus documentos asociados.
/// </summary>
public sealed class DeleteProjectCommandHandler : ICommandHandler<DeleteProjectCommand>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProjectCommandHandler(
        IProjectRepository projectRepository,
        IUnitOfWork unitOfWork)
    {
        _projectRepository = projectRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(
        DeleteProjectCommand request,
        CancellationToken cancellationToken)
    {
        // Obtener el proyecto con sus documentos
        var project = await _projectRepository.GetByIdWithDocumentsAsync(request.Id, cancellationToken);

        if (project is null)
        {
            return Result.Failure(DomainErrors.Project.NotFound);
        }

        // Eliminar el proyecto (cascade delete eliminará documentos y chunks)
        _projectRepository.Remove(project);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
