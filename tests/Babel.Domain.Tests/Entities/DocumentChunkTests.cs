using Babel.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace Babel.Domain.Tests.Entities;

public class DocumentChunkTests
{
    [Fact]
    public void Constructor_ShouldInitializeWithDefaultValues()
    {
        // Act
        var chunk = new DocumentChunk();

        // Assert
        chunk.Id.Should().NotBeEmpty();
        chunk.DocumentId.Should().BeEmpty();
        chunk.ChunkIndex.Should().Be(0);
        chunk.StartCharIndex.Should().Be(0);
        chunk.EndCharIndex.Should().Be(0);
        chunk.Content.Should().BeEmpty();
        chunk.TokenCount.Should().Be(0);
        chunk.QdrantPointId.Should().BeEmpty();
        chunk.PageNumber.Should().BeNull();
        chunk.SectionTitle.Should().BeNull();
        chunk.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        chunk.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void DocumentChunk_ShouldAllowSettingPositionInformation()
    {
        // Arrange
        var chunk = new DocumentChunk();
        var documentId = Guid.NewGuid();

        // Act
        chunk.DocumentId = documentId;
        chunk.ChunkIndex = 5;
        chunk.StartCharIndex = 1000;
        chunk.EndCharIndex = 1500;

        // Assert
        chunk.DocumentId.Should().Be(documentId);
        chunk.ChunkIndex.Should().Be(5);
        chunk.StartCharIndex.Should().Be(1000);
        chunk.EndCharIndex.Should().Be(1500);
    }

    [Fact]
    public void DocumentChunk_ShouldAllowSettingContent()
    {
        // Arrange
        var chunk = new DocumentChunk();
        const string content = "This is the chunk content that will be vectorized.";

        // Act
        chunk.Content = content;
        chunk.TokenCount = 10;

        // Assert
        chunk.Content.Should().Be(content);
        chunk.TokenCount.Should().Be(10);
    }

    [Fact]
    public void DocumentChunk_ShouldAllowSettingQdrantReference()
    {
        // Arrange
        var chunk = new DocumentChunk();
        var qdrantPointId = Guid.NewGuid();

        // Act
        chunk.QdrantPointId = qdrantPointId;

        // Assert
        chunk.QdrantPointId.Should().Be(qdrantPointId);
    }

    [Fact]
    public void DocumentChunk_ShouldAllowSettingOptionalMetadata()
    {
        // Arrange
        var chunk = new DocumentChunk();

        // Act
        chunk.PageNumber = "15";
        chunk.SectionTitle = "Chapter 3: Architecture";

        // Assert
        chunk.PageNumber.Should().Be("15");
        chunk.SectionTitle.Should().Be("Chapter 3: Architecture");
    }

    [Fact]
    public void DocumentChunk_ShouldHaveUniqueIds()
    {
        // Arrange & Act
        var chunk1 = new DocumentChunk();
        var chunk2 = new DocumentChunk();

        // Assert
        chunk1.Id.Should().NotBe(chunk2.Id);
    }

    [Fact]
    public void DocumentChunk_ShouldCalculateContentLength()
    {
        // Arrange
        var chunk = new DocumentChunk
        {
            StartCharIndex = 100,
            EndCharIndex = 600,
            Content = "Some content here"
        };

        // Act
        var length = chunk.EndCharIndex - chunk.StartCharIndex;

        // Assert
        length.Should().Be(500);
    }

    [Fact]
    public void DocumentChunk_ShouldSupportMultipleChunksPerDocument()
    {
        // Arrange
        var document = new Document { FileName = "large_document.pdf" };
        var chunks = new List<DocumentChunk>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            var chunk = new DocumentChunk
            {
                DocumentId = document.Id,
                ChunkIndex = i,
                StartCharIndex = i * 500,
                EndCharIndex = (i + 1) * 500,
                Content = $"Content for chunk {i}",
                TokenCount = 100,
                QdrantPointId = Guid.NewGuid()
            };
            chunks.Add(chunk);
            document.Chunks.Add(chunk);
        }

        // Assert
        document.Chunks.Should().HaveCount(10);
        chunks.Should().OnlyHaveUniqueItems(c => c.Id);
        chunks.Should().OnlyHaveUniqueItems(c => c.QdrantPointId);
        chunks.Select(c => c.ChunkIndex).Should().BeInAscendingOrder();
    }
}
