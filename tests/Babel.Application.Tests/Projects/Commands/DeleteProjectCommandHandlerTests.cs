using Babel.Application.Common;
using Babel.Application.Interfaces;
using Babel.Application.Projects.Commands;
using Babel.Domain.Entities;
using NSubstitute;

namespace Babel.Application.Tests.Projects.Commands;

public class DeleteProjectCommandHandlerTests
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly DeleteProjectCommandHandler _handler;

    public DeleteProjectCommandHandlerTests()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new DeleteProjectCommandHandler(_projectRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WithExistingProject_ShouldDeleteSuccessfully()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var project = new Project
        {
            Id = projectId,
            Name = "Test Project",
            Documents = new List<Document>()
        };

        var command = new DeleteProjectCommand(projectId);
        _projectRepository.GetByIdWithDocumentsAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(project);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _projectRepository.Received(1).Remove(project);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonExistentProject_ShouldReturnFailure()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var command = new DeleteProjectCommand(projectId);

        _projectRepository.GetByIdWithDocumentsAsync(projectId, Arg.Any<CancellationToken>())
            .Returns((Project?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Project.NotFound);
        _projectRepository.DidNotReceive().Remove(Arg.Any<Project>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithProjectWithDocuments_ShouldDeleteWithCascade()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var project = new Project
        {
            Id = projectId,
            Name = "Test Project",
            Documents = new List<Document>
            {
                new() { Id = Guid.NewGuid(), FileName = "doc1.txt" },
                new() { Id = Guid.NewGuid(), FileName = "doc2.pdf" }
            }
        };

        var command = new DeleteProjectCommand(projectId);
        _projectRepository.GetByIdWithDocumentsAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(project);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _projectRepository.Received(1).Remove(project);
    }
}
