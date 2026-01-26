using Babel.Application.Common;
using Babel.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Babel.Application.Projects.Commands;

/// <summary>
/// Handler para DeleteProjectCommand.
/// Elimina un proyecto, sus vectores de Qdrant, archivos del storage y registros de la base de datos.
/// </summary>
public sealed class DeleteProjectCommandHandler : ICommandHandler<DeleteProjectCommand>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IVectorStoreService _vectorStoreService;
    private readonly IStorageService _storageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteProjectCommandHandler> _logger;

    public DeleteProjectCommandHandler(
        IProjectRepository projectRepository,
        IVectorStoreService vectorStoreService,
        IStorageService storageService,
        IUnitOfWork unitOfWork,
        ILogger<DeleteProjectCommandHandler> logger)
    {
        _projectRepository = projectRepository;
        _vectorStoreService = vectorStoreService;
        _storageService = storageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(
        DeleteProjectCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Obtener el proyecto con sus documentos
        var project = await _projectRepository.GetByIdWithDocumentsAsync(request.Id, cancellationToken);

        if (project is null)
        {
            return Result.Failure(DomainErrors.Project.NotFound);
        }

        // 2. Eliminar todos los puntos del proyecto en Qdrant
        var qdrantResult = await _vectorStoreService.DeleteByProjectIdAsync(project.Id, cancellationToken);

        if (qdrantResult.IsFailure)
        {
            _logger.LogWarning(
                "Error eliminando vectores de Qdrant para proyecto {ProjectId}: {Error}. Continuando con eliminación.",
                project.Id, qdrantResult.Error.Description);
        }
        else
        {
            _logger.LogInformation(
                "Vectores eliminados de Qdrant para proyecto {ProjectId}",
                project.Id);
        }

        // 3. Eliminar archivos del storage
        try
        {
            await _storageService.DeleteProjectFilesAsync(project.Id, cancellationToken);
            _logger.LogInformation(
                "Archivos eliminados del storage para proyecto {ProjectId}",
                project.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Error eliminando archivos del storage para proyecto {ProjectId}. Continuando con eliminación.",
                project.Id);
        }

        // 4. Eliminar el proyecto (cascade delete eliminará documentos y chunks)
        _projectRepository.Remove(project);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Proyecto {ProjectId} eliminado completamente", project.Id);

        return Result.Success();
    }
}
