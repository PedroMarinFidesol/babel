using Babel.Application.Behaviors;
using Babel.Application.Common;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using NSubstitute;

namespace Babel.Application.Tests.Behaviors;

// Test request - must be public for NSubstitute to create proxies
public record ValidationTestRequest(string Name) : IRequest<Result<string>>;

public class ValidationBehaviorTests
{
    private class TestRequestValidator : AbstractValidator<ValidationTestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("El nombre es requerido");
        }
    }

    [Fact]
    public async Task Handle_WithNoValidators_ShouldCallNext()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<ValidationTestRequest>>();
        var behavior = new ValidationBehavior<ValidationTestRequest, Result<string>>(validators);
        var request = new ValidationTestRequest("Test");
        var expectedResult = Result.Success("Success");

        RequestHandlerDelegate<Result<string>> next = _ => Task.FromResult(expectedResult);

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldCallNext()
    {
        // Arrange
        var validator = new TestRequestValidator();
        var validators = new List<IValidator<ValidationTestRequest>> { validator };
        var behavior = new ValidationBehavior<ValidationTestRequest, Result<string>>(validators);
        var request = new ValidationTestRequest("Valid Name");
        var expectedResult = Result.Success("Success");

        RequestHandlerDelegate<Result<string>> next = _ => Task.FromResult(expectedResult);

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task Handle_WithInvalidRequest_ShouldThrowValidationException()
    {
        // Arrange
        var validator = new TestRequestValidator();
        var validators = new List<IValidator<ValidationTestRequest>> { validator };
        var behavior = new ValidationBehavior<ValidationTestRequest, Result<string>>(validators);
        var request = new ValidationTestRequest(""); // Invalid - empty name

        RequestHandlerDelegate<Result<string>> next = _ => Task.FromResult(Result.Success("Success"));

        // Act
        Func<Task> act = () => behavior.Handle(request, next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.Any(e => e.ErrorMessage.Contains("nombre")));
    }

    [Fact]
    public async Task Handle_WithMultipleValidators_ShouldRunAllValidators()
    {
        // Arrange
        var validator1 = Substitute.For<IValidator<ValidationTestRequest>>();
        var validator2 = Substitute.For<IValidator<ValidationTestRequest>>();

        validator1.ValidateAsync(Arg.Any<ValidationContext<ValidationTestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        validator2.ValidateAsync(Arg.Any<ValidationContext<ValidationTestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var validators = new List<IValidator<ValidationTestRequest>> { validator1, validator2 };
        var behavior = new ValidationBehavior<ValidationTestRequest, Result<string>>(validators);
        var request = new ValidationTestRequest("Test");

        RequestHandlerDelegate<Result<string>> next = _ => Task.FromResult(Result.Success("Success"));

        // Act
        await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        await validator1.Received(1).ValidateAsync(
            Arg.Any<ValidationContext<ValidationTestRequest>>(),
            Arg.Any<CancellationToken>());
        await validator2.Received(1).ValidateAsync(
            Arg.Any<ValidationContext<ValidationTestRequest>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithMultipleValidationErrors_ShouldThrowWithAllErrors()
    {
        // Arrange
        var validator1 = Substitute.For<IValidator<ValidationTestRequest>>();
        var validator2 = Substitute.For<IValidator<ValidationTestRequest>>();

        validator1.ValidateAsync(Arg.Any<ValidationContext<ValidationTestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { new ValidationFailure("Name", "Error 1") }));
        validator2.ValidateAsync(Arg.Any<ValidationContext<ValidationTestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { new ValidationFailure("Name", "Error 2") }));

        var validators = new List<IValidator<ValidationTestRequest>> { validator1, validator2 };
        var behavior = new ValidationBehavior<ValidationTestRequest, Result<string>>(validators);
        var request = new ValidationTestRequest("Test");

        RequestHandlerDelegate<Result<string>> next = _ => Task.FromResult(Result.Success("Success"));

        // Act
        Func<Task> act = () => behavior.Handle(request, next, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().HaveCount(2);
    }
}
