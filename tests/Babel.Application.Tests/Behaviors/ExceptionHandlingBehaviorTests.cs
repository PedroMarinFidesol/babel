using Babel.Application.Behaviors;
using Babel.Application.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Babel.Application.Tests.Behaviors;

// Test requests - must be public for NSubstitute to create proxies
public record ExceptionTestRequest(string Name) : IRequest<Result<string>>;
public record ExceptionTestRequestNoValue() : IRequest<Result>;

public class ExceptionHandlingBehaviorTests
{
    [Fact]
    public async Task Handle_WithSuccessfulExecution_ShouldReturnResult()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ExceptionHandlingBehavior<ExceptionTestRequest, Result<string>>>>();
        var behavior = new ExceptionHandlingBehavior<ExceptionTestRequest, Result<string>>(logger);
        var request = new ExceptionTestRequest("Test");
        var expectedResult = Result.Success("Success");

        RequestHandlerDelegate<Result<string>> next = _ => Task.FromResult(expectedResult);

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task Handle_WithValidationException_ShouldRethrow()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ExceptionHandlingBehavior<ExceptionTestRequest, Result<string>>>>();
        var behavior = new ExceptionHandlingBehavior<ExceptionTestRequest, Result<string>>(logger);
        var request = new ExceptionTestRequest("Test");

        RequestHandlerDelegate<Result<string>> next = _ => throw new ValidationException("Validation failed");

        // Act
        Func<Task> act = () => behavior.Handle(request, next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_WithUnhandledException_ShouldReturnFailureResult()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ExceptionHandlingBehavior<ExceptionTestRequest, Result<string>>>>();
        var behavior = new ExceptionHandlingBehavior<ExceptionTestRequest, Result<string>>(logger);
        var request = new ExceptionTestRequest("Test");

        RequestHandlerDelegate<Result<string>> next = _ => throw new InvalidOperationException("Unexpected error");

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("UnhandledException");
        result.Error.Description.Should().Contain("Unexpected error");
    }

    [Fact]
    public async Task Handle_WithUnhandledException_ShouldLogError()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ExceptionHandlingBehavior<ExceptionTestRequest, Result<string>>>>();
        var behavior = new ExceptionHandlingBehavior<ExceptionTestRequest, Result<string>>(logger);
        var request = new ExceptionTestRequest("Test");

        RequestHandlerDelegate<Result<string>> next = _ => throw new InvalidOperationException("Unexpected error");

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        logger.ReceivedCalls().Should().HaveCountGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task Handle_WithResultWithoutType_ShouldReturnFailureResult()
    {
        // Arrange
        var logger = Substitute.For<ILogger<ExceptionHandlingBehavior<ExceptionTestRequestNoValue, Result>>>();
        var behavior = new ExceptionHandlingBehavior<ExceptionTestRequestNoValue, Result>(logger);
        var request = new ExceptionTestRequestNoValue();

        RequestHandlerDelegate<Result> next = _ => throw new InvalidOperationException("Unexpected error");

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("UnhandledException");
    }
}
