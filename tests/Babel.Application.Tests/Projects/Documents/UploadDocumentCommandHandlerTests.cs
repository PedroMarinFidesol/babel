using System.Text;
using Babel.Application.Common;
using Babel.Application.Documents.Commands;
using Babel.Application.Interfaces;
using Babel.Domain.Enums;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Babel.Application.Tests.Projects.Documents;

public class UploadDocumentCommandHandlerTests
{
    private readonly IProjectRepository _projectRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IStorageService _storageService;
    private readonly IFileTypeDetector _fileTypeDetector;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDocumentProcessingQueue _processingQueue;
    private readonly UploadDocumentCommandHandler _handler;

    public UploadDocumentCommandHandlerTests()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _documentRepository = Substitute.For<IDocumentRepository>();
        _storageService = Substitute.For<IStorageService>();
        _fileTypeDetector = Substitute.For<IFileTypeDetector>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _processingQueue = Substitute.For<IDocumentProcessingQueue>();

        _handler = new UploadDocumentCommandHandler(
            _projectRepository,
            _documentRepository,
            _storageService,
            _fileTypeDetector,
            _unitOfWork,
            _processingQueue);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldUploadDocumentSuccessfully()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var fileName = "test.pdf";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));
        var filePath = $"{projectId}/test.pdf";

        _projectRepository.ExistsAsync(projectId, Arg.Any<CancellationToken>()).Returns(true);
        _fileTypeDetector.IsSupported(fileName).Returns(true);
        _fileTypeDetector.DetectFileType(fileName).Returns(FileExtensionType.Pdf);
        _fileTypeDetector.GetMimeType(fileName).Returns("application/pdf");
        _fileTypeDetector.RequiresOcr(FileExtensionType.Pdf).Returns(true);
        _storageService.SaveFileAsync(stream, fileName, projectId, Arg.Any<CancellationToken>()).Returns(filePath);
        _storageService.GetFileSizeAsync(filePath, Arg.Any<CancellationToken>()).Returns(1024L);
        _storageService.GetFileHashAsync(filePath, Arg.Any<CancellationToken>()).Returns("abc123");
        _documentRepository.ExistsByHashAsync("abc123", projectId, Arg.Any<CancellationToken>()).Returns(false);

        var command = new UploadDocumentCommand(projectId, fileName, stream);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.FileName.Should().Be(fileName);
        result.Value.ProjectId.Should().Be(projectId);
        result.Value.Status.Should().Be(DocumentStatus.Pending);

        _documentRepository.Received(1).Add(Arg.Any<Babel.Domain.Entities.Document>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        _processingQueue.Received(1).EnqueueTextExtraction(Arg.Any<Guid>());
    }

    [Fact]
    public async Task Handle_WithNonExistentProject_ShouldReturnFailure()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));
        var command = new UploadDocumentCommand(projectId, "test.pdf", stream);

        _projectRepository.ExistsAsync(projectId, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Document.ProjectNotFound");
    }

    [Fact]
    public async Task Handle_WithUnsupportedFileType_ShouldReturnFailure()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));
        var command = new UploadDocumentCommand(projectId, "test.exe", stream);

        _projectRepository.ExistsAsync(projectId, Arg.Any<CancellationToken>()).Returns(true);
        _fileTypeDetector.IsSupported("test.exe").Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Document.UnsupportedFileType");
    }

    [Fact]
    public async Task Handle_WithDuplicateFile_ShouldReturnFailure()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var fileName = "test.pdf";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));
        var filePath = $"{projectId}/test.pdf";

        _projectRepository.ExistsAsync(projectId, Arg.Any<CancellationToken>()).Returns(true);
        _fileTypeDetector.IsSupported(fileName).Returns(true);
        _storageService.SaveFileAsync(stream, fileName, projectId, Arg.Any<CancellationToken>()).Returns(filePath);
        _storageService.GetFileSizeAsync(filePath, Arg.Any<CancellationToken>()).Returns(1024L);
        _storageService.GetFileHashAsync(filePath, Arg.Any<CancellationToken>()).Returns("abc123");
        _documentRepository.ExistsByHashAsync("abc123", projectId, Arg.Any<CancellationToken>()).Returns(true);

        var command = new UploadDocumentCommand(projectId, fileName, stream);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Document.DuplicateFile");

        // Verify duplicate file was deleted
        await _storageService.Received(1).DeleteFileAsync(filePath, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithFileTooLarge_ShouldReturnFailure()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var fileName = "test.pdf";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));

        _projectRepository.ExistsAsync(projectId, Arg.Any<CancellationToken>()).Returns(true);
        _fileTypeDetector.IsSupported(fileName).Returns(true);
        _storageService.SaveFileAsync(stream, fileName, projectId, Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("El archivo excede el tamaño máximo permitido"));

        var command = new UploadDocumentCommand(projectId, fileName, stream);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Document.FileTooLarge");
    }
}
