using Babel.Application.Interfaces;
using Babel.Application.Projects.Queries;
using Babel.Domain.Entities;
using Babel.Domain.Enums;
using NSubstitute;

namespace Babel.Application.Tests.Projects.Queries;

public class SearchProjectsQueryHandlerTests
{
    private readonly IProjectRepository _projectRepository;
    private readonly SearchProjectsQueryHandler _handler;

    public SearchProjectsQueryHandlerTests()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _handler = new SearchProjectsQueryHandler(_projectRepository);
    }

    [Fact]
    public async Task Handle_WithMatchingProjects_ShouldReturnResults()
    {
        // Arrange
        var projects = new List<Project>
        {
            CreateProject("Project Alpha"),
            CreateProject("Project Beta")
        };

        _projectRepository.SearchByNameAsync("Project", Arg.Any<CancellationToken>())
            .Returns(projects);

        var query = new SearchProjectsQuery("Project");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithEmptySearchTerm_ShouldReturnEmptyList()
    {
        // Arrange
        var query = new SearchProjectsQuery("");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        await _projectRepository.DidNotReceive().SearchByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithWhitespaceSearchTerm_ShouldReturnEmptyList()
    {
        // Arrange
        var query = new SearchProjectsQuery("   ");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
        await _projectRepository.DidNotReceive().SearchByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNoMatchingProjects_ShouldReturnEmptyList()
    {
        // Arrange
        _projectRepository.SearchByNameAsync("NonExistent", Arg.Any<CancellationToken>())
            .Returns(new List<Project>());

        var query = new SearchProjectsQuery("NonExistent");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldCalculateDocumentCounts()
    {
        // Arrange
        var project = CreateProject("Test Project");
        project.Documents = new List<Document>
        {
            new() { Status = DocumentStatus.Completed },
            new() { Status = DocumentStatus.Pending },
            new() { Status = DocumentStatus.Processing }
        };

        _projectRepository.SearchByNameAsync("Test", Arg.Any<CancellationToken>())
            .Returns(new List<Project> { project });

        var query = new SearchProjectsQuery("Test");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        var dto = result.Value!.First();
        dto.TotalDocuments.Should().Be(3);
        dto.ProcessedDocuments.Should().Be(1);
        dto.PendingDocuments.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldOrderByUpdatedAtDescending()
    {
        // Arrange
        var oldProject = CreateProject("Old Project");
        oldProject.UpdatedAt = DateTime.UtcNow.AddDays(-10);

        var newProject = CreateProject("New Project");
        newProject.UpdatedAt = DateTime.UtcNow;

        _projectRepository.SearchByNameAsync("Project", Arg.Any<CancellationToken>())
            .Returns(new List<Project> { oldProject, newProject });

        var query = new SearchProjectsQuery("Project");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value![0].Name.Should().Be("New Project");
        result.Value[1].Name.Should().Be("Old Project");
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

        _projectRepository.SearchByNameAsync("Test", Arg.Any<CancellationToken>())
            .Returns(new List<Project> { project });

        var query = new SearchProjectsQuery("Test");

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
