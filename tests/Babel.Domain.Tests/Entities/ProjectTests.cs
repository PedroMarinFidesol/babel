using Babel.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Babel.Domain.Tests.Entities;

public class ProjectTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var project = new Project();

        // Assert
        project.Id.Should().NotBeEmpty();
        project.Name.Should().BeEmpty();
        project.Description.Should().BeNull();
        project.Documents.Should().NotBeNull();
        project.Documents.Should().BeEmpty();
        project.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        project.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Project_ShouldAllowSettingName()
    {
        // Arrange
        var project = new Project();
        const string expectedName = "Test Project";

        // Act
        project.Name = expectedName;

        // Assert
        project.Name.Should().Be(expectedName);
    }

    [Fact]
    public void Project_ShouldAllowSettingDescription()
    {
        // Arrange
        var project = new Project();
        const string expectedDescription = "This is a test project description";

        // Act
        project.Description = expectedDescription;

        // Assert
        project.Description.Should().Be(expectedDescription);
    }

    [Fact]
    public void Project_ShouldAllowAddingDocuments()
    {
        // Arrange
        var project = new Project { Name = "Test Project" };
        var document = new Document
        {
            ProjectId = project.Id,
            FileName = "test.pdf"
        };

        // Act
        project.Documents.Add(document);

        // Assert
        project.Documents.Should().HaveCount(1);
        project.Documents.Should().Contain(document);
    }

    [Fact]
    public void Project_ShouldHaveUniqueIds()
    {
        // Arrange & Act
        var project1 = new Project();
        var project2 = new Project();

        // Assert
        project1.Id.Should().NotBe(project2.Id);
    }
}
