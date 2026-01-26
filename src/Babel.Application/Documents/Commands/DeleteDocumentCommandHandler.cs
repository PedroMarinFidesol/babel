using Babel.Application.Common;
using Babel.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Babel.Application.Documents.Commands;

/// <summary>
/// Handler para DeleteDocumentCommand.
/// Elimina el archivo del storage y el registro de la base de datos.
/// </summary>
public sealed class DeleteDocumentCommandHandler : ICommandHandler<DeleteDocumentCommand>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IStorageService _storageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteDocumentCommandHandler> _logger;

    public DeleteDocumentCommandHandler(
        IDocumentRepository documentRepository,
        IStorageService storageService,
        IUnitOfWork unitOfWork,
        ILogger<DeleteDocumentCommandHandler> logger)
    {
        _documentRepository = documentRepository;
        _storageService = storageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(
        DeleteDocumentCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Obtener documento con sus chunks
        var document = await _documentRepository.GetByIdWithChunksAsync(request.Id, cancellationToken);

        if (document is null)
        {
            return Result.Failure(DomainErrors.Document.NotFound);
        }

        var filePath = document.FilePath;

        // 2. Eliminar de base de datos (cascade eliminará los chunks)
        _documentRepository.Remove(document);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 3. Eliminar archivo del storage
        try
        {
            await _storageService.DeleteFileAsync(filePath, cancellationToken);
        }
        catch (Exception ex)
        {
            // Log del error pero no fallar la operación
            // El registro ya fue eliminado de la BD
            _logger.LogWarning(ex,
                "Error al eliminar archivo del storage: {FilePath}. El documento ya fue eliminado de la BD.",
                filePath);
        }

        return Result.Success();
    }
}
