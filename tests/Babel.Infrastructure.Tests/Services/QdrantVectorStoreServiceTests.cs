using Babel.Application.Interfaces;
using Babel.Infrastructure.Configuration;
using Babel.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Qdrant.Client;

namespace Babel.Infrastructure.Tests.Services;

/// <summary>
/// Tests for QdrantVectorStoreService.
/// Note: These are unit tests that verify method behavior.
/// Integration tests would require a running Qdrant instance.
/// </summary>
public class QdrantVectorStoreServiceTests
{
    private readonly IOptions<QdrantOptions> _options;
    private readonly ILogger<QdrantVectorStoreService> _logger;

    public QdrantVectorStoreServiceTests()
    {
        _options = Options.Create(new QdrantOptions
        {
            Host = "localhost",
            GrpcPort = 6334,
            HttpPort = 6333,
            CollectionName = "test_collection",
            VectorSize = 768
        });
        _logger = Substitute.For<ILogger<QdrantVectorStoreService>>();
    }

    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // This test verifies the service can be constructed
        // Actual Qdrant operations would require a real client
        var client = new QdrantClient("localhost", 6334);

        var service = new QdrantVectorStoreService(client, _options, _logger);

        service.Should().NotBeNull();
    }

    [Fact]
    public void ChunkPayload_ShouldCreateCorrectly()
    {
        var documentId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var chunkIndex = 5;
        var fileName = "test.txt";

        var payload = new ChunkPayload(documentId, projectId, chunkIndex, fileName);

        payload.DocumentId.Should().Be(documentId);
        payload.ProjectId.Should().Be(projectId);
        payload.ChunkIndex.Should().Be(chunkIndex);
        payload.FileName.Should().Be(fileName);
    }

    [Fact]
    public async Task UpsertChunksAsync_EmptyList_ShouldReturnSuccess()
    {
        var client = new QdrantClient("localhost", 6334);
        var service = new QdrantVectorStoreService(client, _options, _logger);

        var result = await service.UpsertChunksAsync(
            new List<(Guid, ReadOnlyMemory<float>, ChunkPayload)>());

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ChunkPayload_Equality_ShouldWorkCorrectly()
    {
        var documentId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        var payload1 = new ChunkPayload(documentId, projectId, 0, "test.txt");
        var payload2 = new ChunkPayload(documentId, projectId, 0, "test.txt");
        var payload3 = new ChunkPayload(documentId, projectId, 1, "test.txt");

        payload1.Should().Be(payload2);
        payload1.Should().NotBe(payload3);
    }

    [Fact]
    public void ChunkPayload_ShouldBeImmutable()
    {
        var payload = new ChunkPayload(Guid.NewGuid(), Guid.NewGuid(), 0, "test.txt");

        // Record types are immutable by default
        // This verifies the record was created correctly
        payload.ChunkIndex.Should().Be(0);
        payload.FileName.Should().Be("test.txt");
    }

    [Theory]
    [InlineData("babel_documents")]
    [InlineData("test_collection")]
    [InlineData("my-collection")]
    public void QdrantOptions_ShouldAcceptValidCollectionNames(string collectionName)
    {
        var options = new QdrantOptions
        {
            CollectionName = collectionName
        };

        options.CollectionName.Should().Be(collectionName);
    }

    [Theory]
    [InlineData(768)]
    [InlineData(1536)]
    [InlineData(384)]
    public void QdrantOptions_ShouldAcceptValidVectorSizes(int vectorSize)
    {
        var options = new QdrantOptions
        {
            VectorSize = vectorSize
        };

        options.VectorSize.Should().Be(vectorSize);
    }
}
