using Babel.Application.Interfaces;
using Babel.Application.Projects.Queries;
using Babel.Domain.Entities;
using Babel.Domain.Enums;
using NSubstitute;

namespace Babel.Application.Tests.Projects.Queries;

public class GetProjectsQueryHandlerTests
{
    private readonly IProjectRepository _projectRepository;
    private readonly GetProjectsQueryHandler _handler;

    public GetProjectsQueryHandlerTests()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _handler = new GetProjectsQueryHandler(_projectRepository);
    }

    [Fact]
    public async Task Handle_ShouldReturnAllProjects()
    {
        // Arrange
        var projects = new List<Project>
        {
            CreateProject("Project 1"),
            CreateProject("Project 2"),
            CreateProject("Project 3")
        };

        _projectRepository.GetAllWithDocumentCountAsync(Arg.Any<CancellationToken>())
            .Returns(projects);

        var query = new GetProjectsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyListWhenNoProjects()
    {
        // Arrange
        _projectRepository.GetAllWithDocumentCountAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Project>());

        var query = new GetProjectsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldCalculateDocumentCountsCorrectly()
    {
        // Arrange
        var project = CreateProject("Test Project");
        project.Documents = new List<Document>
        {
            new() { Status = DocumentStatus.Completed },
            new() { Status = DocumentStatus.Completed },
            new() { Status = DocumentStatus.Pending },
            new() { Status = DocumentStatus.Processing },
            new() { Status = DocumentStatus.PendingReview },
            new() { Status = DocumentStatus.Failed }
        };

        _projectRepository.GetAllWithDocumentCountAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Project> { project });

        var query = new GetProjectsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var dto = result.Value!.First();
        dto.TotalDocuments.Should().Be(6);
        dto.ProcessedDocuments.Should().Be(2);
        dto.PendingDocuments.Should().Be(3); // Pending + Processing + PendingReview
    }

    [Fact]
    public async Task Handle_ShouldOrderByUpdatedAtDescending()
    {
        // Arrange
        var oldProject = CreateProject("Old Project");
        oldProject.UpdatedAt = DateTime.UtcNow.AddDays(-10);

        var newProject = CreateProject("New Project");
        newProject.UpdatedAt = DateTime.UtcNow;

        var middleProject = CreateProject("Middle Project");
        middleProject.UpdatedAt = DateTime.UtcNow.AddDays(-5);

        _projectRepository.GetAllWithDocumentCountAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Project> { oldProject, newProject, middleProject });

        var query = new GetProjectsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value![0].Name.Should().Be("New Project");
        result.Value[1].Name.Should().Be("Middle Project");
        result.Value[2].Name.Should().Be("Old Project");
    }

    [Fact]
    public async Task Handle_ShouldMapAllDtoFields()
    {
        // Arrange
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Test Project",
            Description = "Test Description",
            Documents = new List<Document>(),
            CreatedAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc)
        };

        _projectRepository.GetAllWithDocumentCountAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Project> { project });

        var query = new GetProjectsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var dto = result.Value!.First();
        dto.Id.Should().Be(project.Id);
        dto.Name.Should().Be("Test Project");
        dto.Description.Should().Be("Test Description");
        dto.CreatedAt.Should().Be(project.CreatedAt);
        dto.UpdatedAt.Should().Be(project.UpdatedAt);
    }

    private static Project CreateProject(string name)
    {
        return new Project
        {
            Id = Guid.NewGuid(),
            Name = name,
            Documents = new List<Document>(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
