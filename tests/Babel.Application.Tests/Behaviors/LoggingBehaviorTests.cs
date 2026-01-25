using Babel.Application.Behaviors;
using Babel.Application.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Babel.Application.Tests.Behaviors;

// Test request - must be public for NSubstitute to create proxies
public record LoggingTestRequest(string Name) : IRequest<Result<string>>;

public class LoggingBehaviorTests
{
    [Fact]
    public async Task Handle_ShouldLogStartAndEnd()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingBehavior<LoggingTestRequest, Result<string>>>>();
        var behavior = new LoggingBehavior<LoggingTestRequest, Result<string>>(logger);
        var request = new LoggingTestRequest("Test");
        var expectedResult = Result.Success("Success");

        RequestHandlerDelegate<Result<string>> next = _ => Task.FromResult(expectedResult);

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);

        // Verify logging was called (at least for start and completion)
        logger.ReceivedCalls().Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Handle_WhenExceptionThrown_ShouldLogErrorAndRethrow()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingBehavior<LoggingTestRequest, Result<string>>>>();
        var behavior = new LoggingBehavior<LoggingTestRequest, Result<string>>(logger);
        var request = new LoggingTestRequest("Test");

        RequestHandlerDelegate<Result<string>> next = _ => throw new InvalidOperationException("Test exception");

        // Act
        Func<Task> act = () => behavior.Handle(request, next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Test exception");

        // Verify error was logged
        logger.ReceivedCalls().Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Handle_ShouldCallNextDelegate()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingBehavior<LoggingTestRequest, Result<string>>>>();
        var behavior = new LoggingBehavior<LoggingTestRequest, Result<string>>(logger);
        var request = new LoggingTestRequest("Test");
        var nextWasCalled = false;

        RequestHandlerDelegate<Result<string>> next = _ =>
        {
            nextWasCalled = true;
            return Task.FromResult(Result.Success("Success"));
        };

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        nextWasCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnResultFromNext()
    {
        // Arrange
        var logger = Substitute.For<ILogger<LoggingBehavior<LoggingTestRequest, Result<string>>>>();
        var behavior = new LoggingBehavior<LoggingTestRequest, Result<string>>(logger);
        var request = new LoggingTestRequest("Test");
        var expectedValue = "Expected Result Value";
        var expectedResult = Result.Success(expectedValue);

        RequestHandlerDelegate<Result<string>> next = _ => Task.FromResult(expectedResult);

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedValue);
    }
}
