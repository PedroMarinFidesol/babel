using Babel.Application.Common;
using Babel.Application.Documents.Queries;
using Babel.Application.Interfaces;
using Babel.Domain.Entities;
using Babel.Domain.Enums;
using NSubstitute;

namespace Babel.Application.Tests.Projects.Documents;

public class GetDocumentsByProjectQueryHandlerTests
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly GetDocumentsByProjectQueryHandler _handler;

    public GetDocumentsByProjectQueryHandlerTests()
    {
        _documentRepository = Substitute.For<IDocumentRepository>();
        _projectRepository = Substitute.For<IProjectRepository>();
        _handler = new GetDocumentsByProjectQueryHandler(_documentRepository, _projectRepository);
    }

    [Fact]
    public async Task Handle_WithExistingProject_ShouldReturnDocuments()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var documents = new List<Document>
        {
            CreateDocument(projectId, "doc1.pdf"),
            CreateDocument(projectId, "doc2.txt")
        };

        _projectRepository.ExistsAsync(projectId, Arg.Any<CancellationToken>()).Returns(true);
        _documentRepository.GetByProjectIdAsync(projectId, Arg.Any<CancellationToken>()).Returns(documents);

        var query = new GetDocumentsByProjectQuery(projectId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithNonExistentProject_ShouldReturnFailure()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        _projectRepository.ExistsAsync(projectId, Arg.Any<CancellationToken>()).Returns(false);

        var query = new GetDocumentsByProjectQuery(projectId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Document.ProjectNotFound");
    }

    [Fact]
    public async Task Handle_WithNoDocuments_ShouldReturnEmptyList()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        _projectRepository.ExistsAsync(projectId, Arg.Any<CancellationToken>()).Returns(true);
        _documentRepository.GetByProjectIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(new List<Document>());

        var query = new GetDocumentsByProjectQuery(projectId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldOrderByCreatedAtDescending()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var oldDoc = CreateDocument(projectId, "old.pdf");
        oldDoc.CreatedAt = DateTime.UtcNow.AddDays(-10);

        var newDoc = CreateDocument(projectId, "new.pdf");
        newDoc.CreatedAt = DateTime.UtcNow;

        _projectRepository.ExistsAsync(projectId, Arg.Any<CancellationToken>()).Returns(true);
        _documentRepository.GetByProjectIdAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(new List<Document> { oldDoc, newDoc });

        var query = new GetDocumentsByProjectQuery(projectId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value![0].FileName.Should().Be("new.pdf");
        result.Value[1].FileName.Should().Be("old.pdf");
    }

    private static Document CreateDocument(Guid projectId, string fileName)
    {
        return new Document
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            FileName = fileName,
            FileExtension = Path.GetExtension(fileName),
            FilePath = $"{projectId}/{fileName}",
            FileSizeBytes = 1024,
            MimeType = "application/pdf",
            Status = DocumentStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
