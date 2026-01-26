using Babel.Application.Common;
using Babel.Application.Documents.Queries;
using Babel.Application.Interfaces;
using Babel.Domain.Entities;
using Babel.Domain.Enums;
using NSubstitute;

namespace Babel.Application.Tests.Projects.Documents;

public class GetDocumentByIdQueryHandlerTests
{
    private readonly IDocumentRepository _documentRepository;
    private readonly GetDocumentByIdQueryHandler _handler;

    public GetDocumentByIdQueryHandlerTests()
    {
        _documentRepository = Substitute.For<IDocumentRepository>();
        _handler = new GetDocumentByIdQueryHandler(_documentRepository);
    }

    [Fact]
    public async Task Handle_WithExistingDocument_ShouldReturnDocument()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var document = CreateDocument(documentId, "test.pdf");

        _documentRepository.GetByIdAsync(documentId, Arg.Any<CancellationToken>()).Returns(document);

        var query = new GetDocumentByIdQuery(documentId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(documentId);
        result.Value.FileName.Should().Be("test.pdf");
    }

    [Fact]
    public async Task Handle_WithNonExistentDocument_ShouldReturnFailure()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        _documentRepository.GetByIdAsync(documentId, Arg.Any<CancellationToken>()).Returns((Document?)null);

        var query = new GetDocumentByIdQuery(documentId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Document.NotFound");
    }

    [Fact]
    public async Task Handle_ShouldMapAllDtoFields()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var document = new Document
        {
            Id = documentId,
            ProjectId = projectId,
            FileName = "report.pdf",
            FileExtension = ".pdf",
            FileSizeBytes = 2048,
            MimeType = "application/pdf",
            Status = DocumentStatus.Completed,
            IsVectorized = true,
            CreatedAt = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            ProcessedAt = new DateTime(2024, 1, 1, 10, 5, 0, DateTimeKind.Utc)
        };

        _documentRepository.GetByIdAsync(documentId, Arg.Any<CancellationToken>()).Returns(document);

        var query = new GetDocumentByIdQuery(documentId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var dto = result.Value!;
        dto.Id.Should().Be(documentId);
        dto.ProjectId.Should().Be(projectId);
        dto.FileName.Should().Be("report.pdf");
        dto.FileExtension.Should().Be(".pdf");
        dto.FileSizeBytes.Should().Be(2048);
        dto.MimeType.Should().Be("application/pdf");
        dto.Status.Should().Be(DocumentStatus.Completed);
        dto.IsVectorized.Should().BeTrue();
        dto.CreatedAt.Should().Be(document.CreatedAt);
        dto.ProcessedAt.Should().Be(document.ProcessedAt);
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
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
