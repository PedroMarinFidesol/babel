using Babel.Application.Common;
using Babel.Application.Documents.Commands;
using Babel.Application.Interfaces;
using Babel.Domain.Entities;
using Babel.Domain.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Babel.Application.Tests.Projects.Documents;

public class DeleteDocumentCommandHandlerTests
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IStorageService _storageService;
    private readonly IVectorStoreService _vectorStoreService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteDocumentCommandHandler> _logger;
    private readonly DeleteDocumentCommandHandler _handler;

    public DeleteDocumentCommandHandlerTests()
    {
        _documentRepository = Substitute.For<IDocumentRepository>();
        _storageService = Substitute.For<IStorageService>();
        _vectorStoreService = Substitute.For<IVectorStoreService>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _logger = Substitute.For<ILogger<DeleteDocumentCommandHandler>>();

        _handler = new DeleteDocumentCommandHandler(
            _documentRepository,
            _storageService,
            _vectorStoreService,
            _unitOfWork,
            _logger);
    }

    [Fact]
    public async Task Handle_WithExistingDocument_ShouldDeleteSuccessfully()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var document = CreateDocument(documentId, "test.pdf");

        _documentRepository.GetByIdWithChunksAsync(documentId, Arg.Any<CancellationToken>()).Returns(document);

        var command = new DeleteDocumentCommand(documentId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _documentRepository.Received(1).Remove(document);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _storageService.Received(1).DeleteFileAsync(document.FilePath, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonExistentDocument_ShouldReturnFailure()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        _documentRepository.GetByIdWithChunksAsync(documentId, Arg.Any<CancellationToken>())
            .Returns((Document?)null);

        var command = new DeleteDocumentCommand(documentId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Document.NotFound");

        _documentRepository.DidNotReceive().Remove(Arg.Any<Document>());
    }

    [Fact]
    public async Task Handle_WhenStorageDeleteFails_ShouldStillSucceed()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var document = CreateDocument(documentId, "test.pdf");

        _documentRepository.GetByIdWithChunksAsync(documentId, Arg.Any<CancellationToken>()).Returns(document);
        _storageService.DeleteFileAsync(document.FilePath, Arg.Any<CancellationToken>())
            .Throws(new IOException("File delete failed"));

        var command = new DeleteDocumentCommand(documentId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        // Should still succeed because DB deletion is the priority
        result.IsSuccess.Should().BeTrue();

        // DB operations should have been called
        _documentRepository.Received(1).Remove(document);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldDeleteDocumentWithChunks()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var document = CreateDocument(documentId, "test.pdf");
        document.Chunks = new List<DocumentChunk>
        {
            new() { Id = Guid.NewGuid(), DocumentId = documentId },
            new() { Id = Guid.NewGuid(), DocumentId = documentId }
        };

        _documentRepository.GetByIdWithChunksAsync(documentId, Arg.Any<CancellationToken>()).Returns(document);

        var command = new DeleteDocumentCommand(documentId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _documentRepository.Received(1).Remove(document);
    }

    [Fact]
    public async Task Handle_WithVectorizedDocument_ShouldDeleteFromQdrant()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var document = CreateDocument(documentId, "test.pdf");
        document.IsVectorized = true;

        _documentRepository.GetByIdWithChunksAsync(documentId, Arg.Any<CancellationToken>()).Returns(document);
        _vectorStoreService.DeleteByDocumentIdAsync(documentId, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var command = new DeleteDocumentCommand(documentId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _vectorStoreService.Received(1).DeleteByDocumentIdAsync(documentId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonVectorizedDocument_ShouldNotCallQdrant()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var document = CreateDocument(documentId, "test.pdf");
        document.IsVectorized = false;

        _documentRepository.GetByIdWithChunksAsync(documentId, Arg.Any<CancellationToken>()).Returns(document);

        var command = new DeleteDocumentCommand(documentId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _vectorStoreService.DidNotReceive().DeleteByDocumentIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenQdrantDeleteFails_ShouldStillSucceed()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var document = CreateDocument(documentId, "test.pdf");
        document.IsVectorized = true;

        _documentRepository.GetByIdWithChunksAsync(documentId, Arg.Any<CancellationToken>()).Returns(document);
        _vectorStoreService.DeleteByDocumentIdAsync(documentId, Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Qdrant.Error", "Connection failed")));

        var command = new DeleteDocumentCommand(documentId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _documentRepository.Received(1).Remove(document);
    }

    private static Document CreateDocument(Guid id, string fileName)
    {
        return new Document
        {
            Id = id,
            ProjectId = Guid.NewGuid(),
            FileName = fileName,
            FileExtension = Path.GetExtension(fileName),
            FilePath = $"project/{fileName}",
            FileSizeBytes = 1024,
            MimeType = "application/pdf",
            Status = DocumentStatus.Pending,
            Chunks = new List<DocumentChunk>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
