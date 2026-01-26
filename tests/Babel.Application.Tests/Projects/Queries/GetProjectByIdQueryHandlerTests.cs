using Babel.Application.Common;
using Babel.Application.Interfaces;
using Babel.Application.Projects.Queries;
using Babel.Domain.Entities;
using Babel.Domain.Enums;
using NSubstitute;

namespace Babel.Application.Tests.Projects.Queries;

public class GetProjectByIdQueryHandlerTests
{
    private readonly IProjectRepository _projectRepository;
    private readonly GetProjectByIdQueryHandler _handler;

    public GetProjectByIdQueryHandlerTests()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _handler = new GetProjectByIdQueryHandler(_projectRepository);
    }

    [Fact]
    public async Task Handle_WithExistingProject_ShouldReturnProjectDto()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var project = new Project
        {
            Id = projectId,
            Name = "Test Project",
            Description = "Test Description",
            Documents = new List<Document>(),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };

        _projectRepository.GetByIdWithDocumentsAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var query = new GetProjectByIdQuery(projectId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(projectId);
        result.Value.Name.Should().Be("Test Project");
        result.Value.Description.Should().Be("Test Description");
    }

    [Fact]
    public async Task Handle_WithNonExistentProject_ShouldReturnFailure()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        _projectRepository.GetByIdWithDocumentsAsync(projectId, Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        var query = new GetProjectByIdQuery(projectId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Project.NotFound);
    }

    [Fact]
    public async Task Handle_ShouldCalculateDocumentCounts()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var project = new Project
        {
            Id = projectId,
            Name = "Test Project",
            Documents = new List<Document>
            {
                new() { Status = DocumentStatus.Completed },
                new() { Status = DocumentStatus.Completed },
                new() { Status = DocumentStatus.Completed },
                new() { Status = DocumentStatus.Pending },
                new() { Status = DocumentStatus.Processing },
                new() { Status = DocumentStatus.PendingReview },
                new() { Status = DocumentStatus.Failed }
            }
        };

        _projectRepository.GetByIdWithDocumentsAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var query = new GetProjectByIdQuery(projectId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value!.TotalDocuments.Should().Be(7);
        result.Value.ProcessedDocuments.Should().Be(3);
        result.Value.PendingDocuments.Should().Be(3); // Pending + Processing + PendingReview
    }

    [Fact]
    public async Task Handle_WithProjectWithNoDocuments_ShouldReturnZeroCounts()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var project = new Project
        {
            Id = projectId,
            Name = "Empty Project",
            Documents = new List<Document>()
        };

        _projectRepository.GetByIdWithDocumentsAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var query = new GetProjectByIdQuery(projectId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value!.TotalDocuments.Should().Be(0);
        result.Value.ProcessedDocuments.Should().Be(0);
        result.Value.PendingDocuments.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldMapTimestampsCorrectly()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var createdAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc);

        var project = new Project
        {
            Id = projectId,
            Name = "Test Project",
            Documents = new List<Document>(),
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };

        _projectRepository.GetByIdWithDocumentsAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(project);

        var query = new GetProjectByIdQuery(projectId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value!.CreatedAt.Should().Be(createdAt);
        result.Value.UpdatedAt.Should().Be(updatedAt);
    }
}
