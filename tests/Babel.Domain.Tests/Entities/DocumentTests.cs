using Babel.Domain.Entities;
using Babel.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Babel.Domain.Tests.Entities;

public class DocumentTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var document = new Document();

        // Assert
        document.Id.Should().NotBeEmpty();
        document.ProjectId.Should().BeEmpty();
        document.FileName.Should().BeEmpty();
        document.FileExtension.Should().BeEmpty();
        document.FilePath.Should().BeEmpty();
        document.FileSizeBytes.Should().Be(0);
        document.ContentHash.Should().BeEmpty();
        document.MimeType.Should().BeEmpty();
        document.FileType.Should().Be(FileExtensionType.Unknown);
        document.Status.Should().Be(DocumentStatus.Pending);
        document.RequiresOcr.Should().BeFalse();
        document.OcrReviewed.Should().BeFalse();
        document.ProcessedAt.Should().BeNull();
        document.Content.Should().BeNull();
        document.IsVectorized.Should().BeFalse();
        document.VectorizedAt.Should().BeNull();
        document.Chunks.Should().NotBeNull();
        document.Chunks.Should().BeEmpty();
        document.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        document.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Document_ShouldAllowSettingFileInformation()
    {
        // Arrange
        var document = new Document();
        var projectId = Guid.NewGuid();

        // Act
        document.ProjectId = projectId;
        document.FileName = "report.pdf";
        document.FileExtension = ".pdf";
        document.FilePath = "projects/abc123/report.pdf";
        document.FileSizeBytes = 1024 * 1024; // 1 MB
        document.ContentHash = "abc123hash";
        document.MimeType = "application/pdf";
        document.FileType = FileExtensionType.Pdf;

        // Assert
        document.ProjectId.Should().Be(projectId);
        document.FileName.Should().Be("report.pdf");
        document.FileExtension.Should().Be(".pdf");
        document.FilePath.Should().Be("projects/abc123/report.pdf");
        document.FileSizeBytes.Should().Be(1024 * 1024);
        document.ContentHash.Should().Be("abc123hash");
        document.MimeType.Should().Be("application/pdf");
        document.FileType.Should().Be(FileExtensionType.Pdf);
    }

    [Fact]
    public void Document_ShouldAllowSettingProcessingState()
    {
        // Arrange
        var document = new Document();
        var processedAt = DateTime.UtcNow;

        // Act
        document.Status = DocumentStatus.Completed;
        document.RequiresOcr = true;
        document.OcrReviewed = true;
        document.ProcessedAt = processedAt;
        document.Content = "Extracted text content";

        // Assert
        document.Status.Should().Be(DocumentStatus.Completed);
        document.RequiresOcr.Should().BeTrue();
        document.OcrReviewed.Should().BeTrue();
        document.ProcessedAt.Should().Be(processedAt);
        document.Content.Should().Be("Extracted text content");
    }

    [Fact]
    public void Document_ShouldAllowSettingVectorizationState()
    {
        // Arrange
        var document = new Document();
        var vectorizedAt = DateTime.UtcNow;

        // Act
        document.IsVectorized = true;
        document.VectorizedAt = vectorizedAt;

        // Assert
        document.IsVectorized.Should().BeTrue();
        document.VectorizedAt.Should().Be(vectorizedAt);
    }

    [Fact]
    public void Document_ShouldAllowAddingChunks()
    {
        // Arrange
        var document = new Document { FileName = "test.pdf" };
        var chunk = new DocumentChunk
        {
            DocumentId = document.Id,
            ChunkIndex = 0,
            Content = "First chunk content"
        };

        // Act
        document.Chunks.Add(chunk);

        // Assert
        document.Chunks.Should().HaveCount(1);
        document.Chunks.Should().Contain(chunk);
    }

    [Theory]
    [InlineData(FileExtensionType.TextBased)]
    [InlineData(FileExtensionType.ImageBased)]
    [InlineData(FileExtensionType.Pdf)]
    [InlineData(FileExtensionType.OfficeDocument)]
    public void Document_ShouldAcceptAllFileExtensionTypes(FileExtensionType fileType)
    {
        // Arrange
        var document = new Document();

        // Act
        document.FileType = fileType;

        // Assert
        document.FileType.Should().Be(fileType);
    }

    [Theory]
    [InlineData(DocumentStatus.Pending)]
    [InlineData(DocumentStatus.Processing)]
    [InlineData(DocumentStatus.Completed)]
    [InlineData(DocumentStatus.Failed)]
    [InlineData(DocumentStatus.PendingReview)]
    public void Document_ShouldAcceptAllDocumentStatuses(DocumentStatus status)
    {
        // Arrange
        var document = new Document();

        // Act
        document.Status = status;

        // Assert
        document.Status.Should().Be(status);
    }
}
