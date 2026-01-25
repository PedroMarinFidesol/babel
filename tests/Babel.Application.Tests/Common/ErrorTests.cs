using Babel.Application.Common;

namespace Babel.Application.Tests.Common;

public class ErrorTests
{
    [Fact]
    public void Error_ShouldHaveCodeAndDescription()
    {
        // Arrange
        var code = "Test.Code";
        var description = "Test description";

        // Act
        var error = new Error(code, description);

        // Assert
        error.Code.Should().Be(code);
        error.Description.Should().Be(description);
    }

    [Fact]
    public void None_ShouldHaveEmptyCodeAndDescription()
    {
        // Act
        var error = Error.None;

        // Assert
        error.Code.Should().BeEmpty();
        error.Description.Should().BeEmpty();
    }

    [Fact]
    public void NullValue_ShouldHavePredefinedCodeAndDescription()
    {
        // Act
        var error = Error.NullValue;

        // Assert
        error.Code.Should().Be("Error.NullValue");
        error.Description.Should().NotBeEmpty();
    }

    [Fact]
    public void Validation_ShouldPrefixCodeWithValidation()
    {
        // Arrange
        var code = "FieldRequired";
        var description = "El campo es requerido";

        // Act
        var error = Error.Validation(code, description);

        // Assert
        error.Code.Should().Be("Validation.FieldRequired");
        error.Description.Should().Be(description);
    }

    [Fact]
    public void TwoErrors_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var error1 = new Error("Test.Code", "Test description");
        var error2 = new Error("Test.Code", "Test description");

        // Act & Assert
        error1.Should().Be(error2);
    }

    [Fact]
    public void TwoErrors_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var error1 = new Error("Test.Code1", "Test description");
        var error2 = new Error("Test.Code2", "Test description");

        // Act & Assert
        error1.Should().NotBe(error2);
    }
}
