using Babel.Application.Common;
using Babel.Application.Interfaces;
using Babel.Application.Projects.Commands;
using Babel.Domain.Entities;
using Babel.Domain.Enums;
using NSubstitute;

namespace Babel.Application.Tests.Projects.Commands;

public class UpdateProjectCommandHandlerTests
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UpdateProjectCommandHandler _handler;

    public UpdateProjectCommandHandlerTests()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new UpdateProjectCommandHandler(_projectRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldUpdateProjectSuccessfully()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var existingProject = CreateProject(projectId, "Old Name", "Old Description");
        var command = new UpdateProjectCommand(projectId, "New Name", "New Description");

        _projectRepository.GetByIdWithDocumentsAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(existingProject);
        _projectRepository.ExistsByNameAsync("New Name", projectId, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("New Name");
        result.Value.Description.Should().Be("New Description");

        _projectRepository.Received(1).Update(existingProject);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonExistentProject_ShouldReturnFailure()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var command = new UpdateProjectCommand(projectId, "Name", "Description");

        _projectRepository.GetByIdWithDocumentsAsync(projectId, Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Project.NotFound);

        _projectRepository.DidNotReceive().Update(Arg.Any<Project>());
    }

    [Fact]
    public async Task Handle_WithDuplicateName_ShouldReturnFailure()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var existingProject = CreateProject(projectId, "Old Name", "Description");
        var command = new UpdateProjectCommand(projectId, "Existing Name", "Description");

        _projectRepository.GetByIdWithDocumentsAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(existingProject);
        _projectRepository.ExistsByNameAsync("Existing Name", projectId, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Project.NameAlreadyExists);
    }

    [Fact]
    public async Task Handle_WithSameName_ShouldNotCheckForDuplicates()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var existingProject = CreateProject(projectId, "Same Name", "Old Description");
        var command = new UpdateProjectCommand(projectId, "Same Name", "New Description");

        _projectRepository.GetByIdWithDocumentsAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(existingProject);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _projectRepository.DidNotReceive()
            .ExistsByNameAsync(Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldCalculateDocumentCounts()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var existingProject = CreateProject(projectId, "Name", "Description");
        existingProject.Documents = new List<Document>
        {
            new() { Status = DocumentStatus.Completed },
            new() { Status = DocumentStatus.Completed },
            new() { Status = DocumentStatus.Pending },
            new() { Status = DocumentStatus.Processing }
        };

        var command = new UpdateProjectCommand(projectId, "Name", "Description");
        _projectRepository.GetByIdWithDocumentsAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(existingProject);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Value!.TotalDocuments.Should().Be(4);
        result.Value.ProcessedDocuments.Should().Be(2);
        result.Value.PendingDocuments.Should().Be(2);
    }

    private static Project CreateProject(Guid id, string name, string? description)
    {
        return new Project
        {
            Id = id,
            Name = name,
            Description = description,
            Documents = new List<Document>(),
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
    }
}
