using Babel.Application.Common;
using Babel.Application.Interfaces;
using Babel.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Babel.Infrastructure.Services;

/// <summary>
/// Servicio para extraer texto de documentos según su tipo.
/// </summary>
public class TextExtractionService : ITextExtractionService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IStorageService _storageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TextExtractionService> _logger;

    public TextExtractionService(
        IDocumentRepository documentRepository,
        IStorageService storageService,
        IUnitOfWork unitOfWork,
        ILogger<TextExtractionService> logger)
    {
        _documentRepository = documentRepository;
        _storageService = storageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<string>> ExtractTextAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting text extraction for document {DocumentId}", documentId);

        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken);
        if (document is null)
        {
            return Result.Failure<string>(DomainErrors.Document.NotFound);
        }

        if (document.Status == DocumentStatus.Completed)
        {
            _logger.LogInformation("Document {DocumentId} already processed, returning existing content", documentId);
            return Result<string>.Success(document.Content ?? string.Empty);
        }

        try
        {
            document.Status = DocumentStatus.Processing;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            string extractedText;

            switch (document.FileType)
            {
                case FileExtensionType.TextBased:
                    extractedText = await ExtractFromTextFileAsync(document.FilePath, cancellationToken);
                    break;

                case FileExtensionType.Pdf:
                    // PDF puede requerir OCR si es escaneado
                    if (document.RequiresOcr)
                    {
                        _logger.LogInformation("Document {DocumentId} is a scanned PDF, requires OCR", documentId);
                        document.Status = DocumentStatus.Pending;
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        return Result.Failure<string>(DomainErrors.Document.RequiresOcr);
                    }
                    extractedText = await ExtractFromPdfAsync(document.FilePath, cancellationToken);
                    break;

                case FileExtensionType.OfficeDocument:
                    extractedText = await ExtractFromOfficeDocumentAsync(document.FilePath, document.FileExtension, cancellationToken);
                    break;

                case FileExtensionType.ImageBased:
                    _logger.LogInformation("Document {DocumentId} is an image, requires OCR", documentId);
                    document.Status = DocumentStatus.Pending;
                    document.RequiresOcr = true;
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    return Result.Failure<string>(DomainErrors.Document.RequiresOcr);

                default:
                    document.Status = DocumentStatus.Failed;
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    return Result.Failure<string>(DomainErrors.Document.UnsupportedFileType);
            }

            document.Content = extractedText;
            document.Status = DocumentStatus.Completed;
            document.ProcessedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully extracted {CharCount} characters from document {DocumentId}",
                extractedText.Length, documentId);

            return Result<string>.Success(extractedText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from document {DocumentId}", documentId);

            document.Status = DocumentStatus.Failed;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Failure<string>(DomainErrors.Document.ExtractionFailed);
        }
    }

    private async Task<string> ExtractFromTextFileAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = await _storageService.GetFileAsync(filePath, cancellationToken);
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private Task<string> ExtractFromPdfAsync(string filePath, CancellationToken cancellationToken)
    {
        // TODO: Implementar extracción de PDF con biblioteca como PdfPig o iTextSharp
        _logger.LogWarning("PDF text extraction not yet implemented for {FilePath}", filePath);
        return Task.FromResult(string.Empty);
    }

    private Task<string> ExtractFromOfficeDocumentAsync(string filePath, string fileExtension, CancellationToken cancellationToken)
    {
        // TODO: Implementar extracción de Office con biblioteca como DocumentFormat.OpenXml
        _logger.LogWarning("Office document extraction not yet implemented for {FilePath} ({FileExtension})", filePath, fileExtension);
        return Task.FromResult(string.Empty);
    }
}
