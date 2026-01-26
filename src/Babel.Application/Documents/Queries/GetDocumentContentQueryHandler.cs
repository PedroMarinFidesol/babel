using Babel.Application.Common;
using Babel.Application.Interfaces;

namespace Babel.Application.Documents.Queries;

/// <summary>
/// Handler para GetDocumentContentQuery.
/// Obtiene el stream del archivo para descarga.
/// </summary>
public sealed class GetDocumentContentQueryHandler : IQueryHandler<GetDocumentContentQuery, DocumentContentResult>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IStorageService _storageService;

    public GetDocumentContentQueryHandler(
        IDocumentRepository documentRepository,
        IStorageService storageService)
    {
        _documentRepository = documentRepository;
        _storageService = storageService;
    }

    public async Task<Result<DocumentContentResult>> Handle(
        GetDocumentContentQuery request,
        CancellationToken cancellationToken)
    {
        // 1. Obtener metadatos del documento
        var document = await _documentRepository.GetByIdAsync(request.Id, cancellationToken);

        if (document is null)
        {
            return Result.Failure<DocumentContentResult>(DomainErrors.Document.NotFound);
        }

        // 2. Verificar que el archivo existe
        var fileExists = await _storageService.ExistsAsync(document.FilePath, cancellationToken);
        if (!fileExists)
        {
            return Result.Failure<DocumentContentResult>(DomainErrors.Document.NotFound);
        }

        // 3. Obtener stream del archivo
        var stream = await _storageService.GetFileAsync(document.FilePath, cancellationToken);

        var result = new DocumentContentResult
        {
            Content = stream,
            FileName = document.FileName,
            MimeType = document.MimeType,
            FileSizeBytes = document.FileSizeBytes
        };

        return Result.Success(result);
    }
}
