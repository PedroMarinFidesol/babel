using Babel.Application.Common;
using Babel.Application.Interfaces;
using Babel.Application.Projects.Commands;
using Babel.Domain.Entities;
using NSubstitute;

namespace Babel.Application.Tests.Projects.Commands;

public class CreateProjectCommandHandlerTests
{
    private readonly IProjectRepository _projectRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CreateProjectCommandHandler _handler;

    public CreateProjectCommandHandlerTests()
    {
        _projectRepository = Substitute.For<IProjectRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new CreateProjectCommandHandler(_projectRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateProjectSuccessfully()
    {
        // Arrange
        var command = new CreateProjectCommand("Test Project", "Test Description");
        _projectRepository.ExistsByNameAsync(command.Name, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Test Project");
        result.Value.Description.Should().Be("Test Description");
        result.Value.TotalDocuments.Should().Be(0);

        _projectRepository.Received(1).Add(Arg.Is<Project>(p =>
            p.Name == "Test Project" &&
            p.Description == "Test Description"));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDuplicateName_ShouldReturnFailure()
    {
        // Arrange
        var command = new CreateProjectCommand("Existing Project", "Description");
        _projectRepository.ExistsByNameAsync(command.Name, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(DomainErrors.Project.NameAlreadyExists);

        _projectRepository.DidNotReceive().Add(Arg.Any<Project>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNullDescription_ShouldCreateProjectSuccessfully()
    {
        // Arrange
        var command = new CreateProjectCommand("Test Project", null);
        _projectRepository.ExistsByNameAsync(command.Name, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Description.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldTrimNameAndDescription()
    {
        // Arrange
        var command = new CreateProjectCommand("  Test Project  ", "  Description  ");
        _projectRepository.ExistsByNameAsync(command.Name, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Test Project");
        result.Value.Description.Should().Be("Description");
    }

    [Fact]
    public async Task Handle_ShouldSetTimestamps()
    {
        // Arrange
        var command = new CreateProjectCommand("Test Project", "Description");
        _projectRepository.ExistsByNameAsync(command.Name, Arg.Any<CancellationToken>())
            .Returns(false);
        var beforeTest = DateTime.UtcNow;

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);
        var afterTest = DateTime.UtcNow;

        // Assert
        result.Value!.CreatedAt.Should().BeOnOrAfter(beforeTest);
        result.Value.CreatedAt.Should().BeOnOrBefore(afterTest);
        result.Value.UpdatedAt.Should().BeOnOrAfter(beforeTest);
        result.Value.UpdatedAt.Should().BeOnOrBefore(afterTest);
    }
}
