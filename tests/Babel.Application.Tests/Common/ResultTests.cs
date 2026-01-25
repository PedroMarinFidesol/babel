using Babel.Application.Common;

namespace Babel.Application.Tests.Common;

public class ResultTests
{
    [Fact]
    public void Success_ShouldReturnIsSuccessTrue()
    {
        // Act
        var result = Result.Success();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void Failure_ShouldReturnIsFailureTrueAndError()
    {
        // Arrange
        var error = new Error("Test.Error", "Test error message");

        // Act
        var result = Result.Failure(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void SuccessWithValue_ShouldReturnValueAndIsSuccessTrue()
    {
        // Arrange
        var value = "test value";

        // Act
        var result = Result.Success(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
        result.Error.Should().Be(Error.None);
    }

    [Fact]
    public void FailureWithType_ShouldReturnIsFailureTrueAndError()
    {
        // Arrange
        var error = new Error("Test.Error", "Test error message");

        // Act
        var result = Result.Failure<string>(error);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void Value_WhenFailure_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var error = new Error("Test.Error", "Test error message");
        var result = Result.Failure<string>(error);

        // Act
        Action act = () => _ = result.Value;

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*resultado fallido*");
    }

    [Fact]
    public void GetValueOrDefault_WhenSuccess_ShouldReturnValue()
    {
        // Arrange
        var value = "test value";
        var result = Result.Success(value);

        // Act
        var returnedValue = result.GetValueOrDefault();

        // Assert
        returnedValue.Should().Be(value);
    }

    [Fact]
    public void GetValueOrDefault_WhenFailure_ShouldReturnDefault()
    {
        // Arrange
        var error = new Error("Test.Error", "Test error message");
        var result = Result.Failure<string>(error);

        // Act
        var returnedValue = result.GetValueOrDefault();

        // Assert
        returnedValue.Should().BeNull();
    }

    [Fact]
    public void ImplicitConversion_ShouldWrapValueInSuccess()
    {
        // Arrange
        var value = "test value";

        // Act
        Result<string> result = value;

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
    }

    [Fact]
    public void ImplicitConversion_WithNull_ShouldReturnFailure()
    {
        // Arrange
        string? value = null;

        // Act
        Result<string> result = value;

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.NullValue);
    }

    [Fact]
    public void Create_WithValue_ShouldReturnSuccess()
    {
        // Arrange
        var value = "test value";

        // Act
        var result = Result.Create(value);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(value);
    }

    [Fact]
    public void Create_WithNull_ShouldReturnFailure()
    {
        // Arrange
        string? value = null;

        // Act
        var result = Result.Create(value);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(Error.NullValue);
    }
}
