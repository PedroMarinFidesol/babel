using Babel.Application.Common;
using Babel.Application.DTOs;
using Babel.Application.Interfaces;
using Babel.Domain.Entities;
using Babel.Domain.Enums;

namespace Babel.Application.Documents.Commands;

/// <summary>
/// Handler para UploadDocumentCommand.
/// Guarda el archivo en storage, crea el registro en base de datos y encola procesamiento.
/// </summary>
public sealed class UploadDocumentCommandHandler : ICommandHandler<UploadDocumentCommand, DocumentDto>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IStorageService _storageService;
    private readonly IFileTypeDetector _fileTypeDetector;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDocumentProcessingQueue? _processingQueue;

    public UploadDocumentCommandHandler(
        IProjectRepository projectRepository,
        IDocumentRepository documentRepository,
        IStorageService storageService,
        IFileTypeDetector fileTypeDetector,
        IUnitOfWork unitOfWork,
        IDocumentProcessingQueue? processingQueue = null)
    {
        _projectRepository = projectRepository;
        _documentRepository = documentRepository;
        _storageService = storageService;
        _fileTypeDetector = fileTypeDetector;
        _unitOfWork = unitOfWork;
        _processingQueue = processingQueue;
    }

    public async Task<Result<DocumentDto>> Handle(
        UploadDocumentCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Verificar que el proyecto existe
        var projectExists = await _projectRepository.ExistsAsync(request.ProjectId, cancellationToken);
        if (!projectExists)
        {
            return Result.Failure<DocumentDto>(DomainErrors.Document.ProjectNotFound);
        }

        // 2. Verificar que el tipo de archivo está soportado
        if (!_fileTypeDetector.IsSupported(request.FileName))
        {
            return Result.Failure<DocumentDto>(DomainErrors.Document.UnsupportedFileType);
        }

        // 3. Guardar archivo en storage
        string filePath;
        try
        {
            filePath = await _storageService.SaveFileAsync(
                request.FileStream,
                request.FileName,
                request.ProjectId,
                cancellationToken);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("tamaño máximo"))
        {
            return Result.Failure<DocumentDto>(DomainErrors.Document.FileTooLarge);
        }

        // 4. Obtener información del archivo guardado
        var fileSize = await _storageService.GetFileSizeAsync(filePath, cancellationToken);
        var contentHash = await _storageService.GetFileHashAsync(filePath, cancellationToken);

        // 5. Verificar duplicados por hash
        var isDuplicate = await _documentRepository.ExistsByHashAsync(
            contentHash,
            request.ProjectId,
            cancellationToken);

        if (isDuplicate)
        {
            // Eliminar el archivo guardado ya que es duplicado
            await _storageService.DeleteFileAsync(filePath, cancellationToken);
            return Result.Failure<DocumentDto>(DomainErrors.Document.DuplicateFile);
        }

        // 6. Detectar tipo de archivo
        var fileType = _fileTypeDetector.DetectFileType(request.FileName);
        var mimeType = _fileTypeDetector.GetMimeType(request.FileName);
        var requiresOcr = _fileTypeDetector.RequiresOcr(fileType);

        // 7. Crear entidad Document
        var document = new Document
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            FileName = Path.GetFileName(request.FileName),
            FileExtension = Path.GetExtension(request.FileName).ToLowerInvariant(),
            FilePath = filePath,
            FileSizeBytes = fileSize,
            ContentHash = contentHash,
            MimeType = mimeType,
            FileType = fileType,
            Status = DocumentStatus.Pending,
            RequiresOcr = requiresOcr,
            OcrReviewed = false,
            IsVectorized = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // 8. Guardar en base de datos
        _documentRepository.Add(document);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 9. Encolar procesamiento en segundo plano
        _processingQueue?.EnqueueTextExtraction(document.Id);

        // 10. Mapear a DTO y retornar
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
