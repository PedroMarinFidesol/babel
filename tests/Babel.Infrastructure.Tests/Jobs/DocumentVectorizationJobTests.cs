using Babel.Application.Common;
using Babel.Application.Interfaces;
using Babel.Domain.Entities;
using Babel.Domain.Enums;
using Babel.Infrastructure.Jobs;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Babel.Infrastructure.Tests.Jobs;

public class DocumentVectorizationJobTests
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IChunkingService _chunkingService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStoreService _vectorStoreService;
    private readonly ILogger<DocumentVectorizationJob> _logger;
    private readonly DocumentVectorizationJob _job;

    public DocumentVectorizationJobTests()
    {
        _documentRepository = Substitute.For<IDocumentRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _chunkingService = Substitute.For<IChunkingService>();
        _embeddingService = Substitute.For<IEmbeddingService>();
        _vectorStoreService = Substitute.For<IVectorStoreService>();
        _logger = Substitute.For<ILogger<DocumentVectorizationJob>>();

        _job = new DocumentVectorizationJob(
            _documentRepository,
            _unitOfWork,
            _chunkingService,
            _embeddingService,
            _vectorStoreService,
            _logger);
    }

    private Document CreateTestDocument(
        Guid? id = null,
        string content = "Test content for vectorization.",
        DocumentStatus status = DocumentStatus.Completed,
        bool isVectorized = false)
    {
        return new Document
        {
            Id = id ?? Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            FileName = "test.txt",
            FileExtension = ".txt",
            FilePath = "/test/test.txt",
            FileSizeBytes = 1000,
            ContentHash = "hash123",
            MimeType = "text/plain",
            FileType = FileExtensionType.TextBased,
            Content = content,
            Status = status,
            IsVectorized = isVectorized,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private ChunkResult CreateChunkResult(int index, string content)
    {
        return new ChunkResult(
            ChunkIndex: index,
            Content: content,
            StartCharIndex: index * 100,
            EndCharIndex: (index + 1) * 100 - 1,
            EstimatedTokenCount: content.Length / 4);
    }

    private static IReadOnlyList<ReadOnlyMemory<float>> CreateEmbeddings(int count)
    {
        var embeddings = new List<ReadOnlyMemory<float>>();
        for (int i = 0; i < count; i++)
        {
            embeddings.Add(new float[] { 0.1f + i * 0.1f, 0.2f + i * 0.1f, 0.3f + i * 0.1f });
        }
        return embeddings;
    }

    [Fact]
    public async Task ProcessAsync_DocumentNotFound_ShouldThrowException()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        _documentRepository.GetByIdWithChunksAsync(documentId, Arg.Any<CancellationToken>())
            .Returns((Document?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _job.ProcessAsync(documentId));

        exception.Message.Should().Contain(documentId.ToString());
    }

    [Fact]
    public async Task ProcessAsync_DocumentWithoutContent_ShouldThrowException()
    {
        // Arrange
        var document = CreateTestDocument(content: "");
        _documentRepository.GetByIdWithChunksAsync(document.Id, Arg.Any<CancellationToken>())
            .Returns(document);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _job.ProcessAsync(document.Id));

        exception.Message.Should().Contain("no tiene contenido");
    }

    [Fact]
    public async Task ProcessAsync_DocumentWithWhitespaceOnlyContent_ShouldThrowException()
    {
        // Arrange
        var document = CreateTestDocument(content: "   \t\n  ");
        _documentRepository.GetByIdWithChunksAsync(document.Id, Arg.Any<CancellationToken>())
            .Returns(document);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _job.ProcessAsync(document.Id));

        exception.Message.Should().Contain("no tiene contenido");
    }

    [Fact]
    public async Task ProcessAsync_DocumentNotCompleted_ShouldThrowException()
    {
        // Arrange
        var document = CreateTestDocument(status: DocumentStatus.Processing);
        _documentRepository.GetByIdWithChunksAsync(document.Id, Arg.Any<CancellationToken>())
            .Returns(document);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _job.ProcessAsync(document.Id));

        exception.Message.Should().Contain("Completed");
    }

    [Fact]
    public async Task ProcessAsync_ChunkingReturnsEmpty_ShouldThrowException()
    {
        // Arrange
        var document = CreateTestDocument();
        _documentRepository.GetByIdWithChunksAsync(document.Id, Arg.Any<CancellationToken>())
            .Returns(document);
        _chunkingService.ChunkText(document.Content!, document.Id)
            .Returns(Array.Empty<ChunkResult>());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _job.ProcessAsync(document.Id));

        exception.Message.Should().Contain("no produjo resultados");
    }

    [Fact]
    public async Task ProcessAsync_EmbeddingFails_ShouldThrowException()
    {
        // Arrange
        var document = CreateTestDocument();
        var chunks = new List<ChunkResult>
        {
            CreateChunkResult(0, "Chunk 0 content"),
            CreateChunkResult(1, "Chunk 1 content")
        };

        _documentRepository.GetByIdWithChunksAsync(document.Id, Arg.Any<CancellationToken>())
            .Returns(document);
        _chunkingService.ChunkText(document.Content!, document.Id)
            .Returns(chunks);
        _embeddingService.GenerateEmbeddingsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<ReadOnlyMemory<float>>>(
                DomainErrors.Vectorization.EmbeddingFailed));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _job.ProcessAsync(document.Id));

        exception.Message.Should().Contain("Error generando embeddings");
    }

    [Fact]
    public async Task ProcessAsync_QdrantUpsertFails_ShouldThrowException()
    {
        // Arrange
        var document = CreateTestDocument();
        var chunks = new List<ChunkResult>
        {
            CreateChunkResult(0, "Chunk content")
        };

        _documentRepository.GetByIdWithChunksAsync(document.Id, Arg.Any<CancellationToken>())
            .Returns(document);
        _chunkingService.ChunkText(document.Content!, document.Id)
            .Returns(chunks);
        _embeddingService.GenerateEmbeddingsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<ReadOnlyMemory<float>>>(CreateEmbeddings(1)));
        _vectorStoreService.UpsertChunksAsync(Arg.Any<IReadOnlyList<(Guid, ReadOnlyMemory<float>, ChunkPayload)>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(DomainErrors.Vectorization.QdrantOperationFailed));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _job.ProcessAsync(document.Id));

        exception.Message.Should().Contain("Error guardando en Qdrant");
    }

    [Fact]
    public async Task ProcessAsync_SuccessfulVectorization_ShouldSaveChunksAndMarkAsVectorized()
    {
        // Arrange
        var document = CreateTestDocument();
        var chunks = new List<ChunkResult>
        {
            CreateChunkResult(0, "First chunk content"),
            CreateChunkResult(1, "Second chunk content")
        };

        _documentRepository.GetByIdWithChunksAsync(document.Id, Arg.Any<CancellationToken>())
            .Returns(document);
        _chunkingService.ChunkText(document.Content!, document.Id)
            .Returns(chunks);
        _embeddingService.GenerateEmbeddingsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<ReadOnlyMemory<float>>>(CreateEmbeddings(2)));
        _embeddingService.GetVectorDimension().Returns(3);
        _vectorStoreService.UpsertChunksAsync(Arg.Any<IReadOnlyList<(Guid, ReadOnlyMemory<float>, ChunkPayload)>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        await _job.ProcessAsync(document.Id);

        // Assert
        document.Chunks.Should().HaveCount(2);
        document.IsVectorized.Should().BeTrue();
        document.VectorizedAt.Should().NotBeNull();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _vectorStoreService.Received(1).UpsertChunksAsync(
            Arg.Is<IReadOnlyList<(Guid, ReadOnlyMemory<float>, ChunkPayload)>>(x => x.Count == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_ReVectorization_ShouldDeleteExistingChunksFirst()
    {
        // Arrange
        var document = CreateTestDocument(isVectorized: true);
        document.Chunks.Add(new DocumentChunk
        {
            Id = Guid.NewGuid(),
            DocumentId = document.Id,
            ChunkIndex = 0,
            Content = "Old chunk",
            QdrantPointId = Guid.NewGuid(),
            StartCharIndex = 0,
            EndCharIndex = 10,
            TokenCount = 3,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var chunks = new List<ChunkResult>
        {
            CreateChunkResult(0, "New chunk content")
        };

        _documentRepository.GetByIdWithChunksAsync(document.Id, Arg.Any<CancellationToken>())
            .Returns(document);
        _chunkingService.ChunkText(document.Content!, document.Id)
            .Returns(chunks);
        _embeddingService.GenerateEmbeddingsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<ReadOnlyMemory<float>>>(CreateEmbeddings(1)));
        _embeddingService.GetVectorDimension().Returns(3);
        _vectorStoreService.DeleteByDocumentIdAsync(document.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        _vectorStoreService.UpsertChunksAsync(Arg.Any<IReadOnlyList<(Guid, ReadOnlyMemory<float>, ChunkPayload)>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        await _job.ProcessAsync(document.Id);

        // Assert
        await _vectorStoreService.Received(1).DeleteByDocumentIdAsync(document.Id, Arg.Any<CancellationToken>());
        document.Chunks.Should().HaveCount(1);
        document.Chunks.First().Content.Should().Be("New chunk content");
    }

    [Fact]
    public async Task ProcessAsync_ShouldCreateDocumentChunksWithCorrectProperties()
    {
        // Arrange
        var document = CreateTestDocument();
        var chunks = new List<ChunkResult>
        {
            new ChunkResult(
                ChunkIndex: 0,
                Content: "Test chunk content",
                StartCharIndex: 0,
                EndCharIndex: 17,
                EstimatedTokenCount: 5)
        };

        _documentRepository.GetByIdWithChunksAsync(document.Id, Arg.Any<CancellationToken>())
            .Returns(document);
        _chunkingService.ChunkText(document.Content!, document.Id)
            .Returns(chunks);
        _embeddingService.GenerateEmbeddingsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<ReadOnlyMemory<float>>>(CreateEmbeddings(1)));
        _embeddingService.GetVectorDimension().Returns(3);
        _vectorStoreService.UpsertChunksAsync(Arg.Any<IReadOnlyList<(Guid, ReadOnlyMemory<float>, ChunkPayload)>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        await _job.ProcessAsync(document.Id);

        // Assert
        var createdChunk = document.Chunks.First();
        createdChunk.DocumentId.Should().Be(document.Id);
        createdChunk.ChunkIndex.Should().Be(0);
        createdChunk.Content.Should().Be("Test chunk content");
        createdChunk.StartCharIndex.Should().Be(0);
        createdChunk.EndCharIndex.Should().Be(17);
        createdChunk.TokenCount.Should().Be(5);
        createdChunk.QdrantPointId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ProcessAsync_ShouldPassCorrectPayloadToQdrant()
    {
        // Arrange
        var document = CreateTestDocument();
        var chunks = new List<ChunkResult>
        {
            CreateChunkResult(0, "Chunk content")
        };

        _documentRepository.GetByIdWithChunksAsync(document.Id, Arg.Any<CancellationToken>())
            .Returns(document);
        _chunkingService.ChunkText(document.Content!, document.Id)
            .Returns(chunks);
        _embeddingService.GenerateEmbeddingsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<ReadOnlyMemory<float>>>(CreateEmbeddings(1)));
        _embeddingService.GetVectorDimension().Returns(3);

        IReadOnlyList<(Guid PointId, ReadOnlyMemory<float> Embedding, ChunkPayload Payload)>? capturedChunks = null;
        _vectorStoreService.UpsertChunksAsync(
            Arg.Do<IReadOnlyList<(Guid, ReadOnlyMemory<float>, ChunkPayload)>>(x => capturedChunks = x),
            Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        await _job.ProcessAsync(document.Id);

        // Assert
        capturedChunks.Should().NotBeNull();
        capturedChunks.Should().HaveCount(1);
        var payload = capturedChunks![0].Payload;
        payload.DocumentId.Should().Be(document.Id);
        payload.ProjectId.Should().Be(document.ProjectId);
        payload.ChunkIndex.Should().Be(0);
        payload.FileName.Should().Be(document.FileName);
    }

    [Fact]
    public async Task ProcessAsync_QdrantSaveBeforeSqlServer_ShouldNotSaveToSqlIfQdrantFails()
    {
        // Arrange
        var document = CreateTestDocument();
        var chunks = new List<ChunkResult>
        {
            CreateChunkResult(0, "Chunk content")
        };

        _documentRepository.GetByIdWithChunksAsync(document.Id, Arg.Any<CancellationToken>())
            .Returns(document);
        _chunkingService.ChunkText(document.Content!, document.Id)
            .Returns(chunks);
        _embeddingService.GenerateEmbeddingsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<ReadOnlyMemory<float>>>(CreateEmbeddings(1)));
        _vectorStoreService.UpsertChunksAsync(Arg.Any<IReadOnlyList<(Guid, ReadOnlyMemory<float>, ChunkPayload)>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(DomainErrors.Vectorization.QdrantOperationFailed));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _job.ProcessAsync(document.Id));

        // Verify SaveChangesAsync was NOT called because Qdrant failed first
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_MultipleChunks_ShouldGenerateBatchEmbeddings()
    {
        // Arrange
        var document = CreateTestDocument(content: "Long content that will be split into multiple chunks.");
        var chunks = new List<ChunkResult>
        {
            CreateChunkResult(0, "First chunk"),
            CreateChunkResult(1, "Second chunk"),
            CreateChunkResult(2, "Third chunk")
        };

        _documentRepository.GetByIdWithChunksAsync(document.Id, Arg.Any<CancellationToken>())
            .Returns(document);
        _chunkingService.ChunkText(document.Content!, document.Id)
            .Returns(chunks);
        _embeddingService.GenerateEmbeddingsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<ReadOnlyMemory<float>>>(CreateEmbeddings(3)));
        _embeddingService.GetVectorDimension().Returns(3);
        _vectorStoreService.UpsertChunksAsync(Arg.Any<IReadOnlyList<(Guid, ReadOnlyMemory<float>, ChunkPayload)>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        await _job.ProcessAsync(document.Id);

        // Assert
        await _embeddingService.Received(1).GenerateEmbeddingsAsync(
            Arg.Is<IReadOnlyList<string>>(x => x.Count == 3),
            Arg.Any<CancellationToken>());
        document.Chunks.Should().HaveCount(3);
    }

    [Fact]
    public async Task ProcessAsync_VectorizedAt_ShouldBeSetToCurrentTime()
    {
        // Arrange
        var document = CreateTestDocument();
        var beforeTest = DateTime.UtcNow;
        var chunks = new List<ChunkResult>
        {
            CreateChunkResult(0, "Chunk content")
        };

        _documentRepository.GetByIdWithChunksAsync(document.Id, Arg.Any<CancellationToken>())
            .Returns(document);
        _chunkingService.ChunkText(document.Content!, document.Id)
            .Returns(chunks);
        _embeddingService.GenerateEmbeddingsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<ReadOnlyMemory<float>>>(CreateEmbeddings(1)));
        _embeddingService.GetVectorDimension().Returns(3);
        _vectorStoreService.UpsertChunksAsync(Arg.Any<IReadOnlyList<(Guid, ReadOnlyMemory<float>, ChunkPayload)>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        await _job.ProcessAsync(document.Id);
        var afterTest = DateTime.UtcNow;

        // Assert
        document.VectorizedAt.Should().NotBeNull();
        document.VectorizedAt!.Value.Should().BeOnOrAfter(beforeTest);
        document.VectorizedAt!.Value.Should().BeOnOrBefore(afterTest);
    }

    [Fact]
    public async Task ProcessAsync_DocumentPending_ShouldThrowWithCorrectMessage()
    {
        // Arrange
        var document = CreateTestDocument(status: DocumentStatus.Pending);
        _documentRepository.GetByIdWithChunksAsync(document.Id, Arg.Any<CancellationToken>())
            .Returns(document);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _job.ProcessAsync(document.Id));

        exception.Message.Should().Contain("Pending");
    }

    [Fact]
    public async Task ProcessAsync_DocumentFailed_ShouldThrowWithCorrectMessage()
    {
        // Arrange
        var document = CreateTestDocument(status: DocumentStatus.Failed);
        _documentRepository.GetByIdWithChunksAsync(document.Id, Arg.Any<CancellationToken>())
            .Returns(document);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _job.ProcessAsync(document.Id));

        exception.Message.Should().Contain("Failed");
    }

    [Fact]
    public async Task ProcessAsync_EachChunkShouldHaveUniqueQdrantPointId()
    {
        // Arrange
        var document = CreateTestDocument();
        var chunks = new List<ChunkResult>
        {
            CreateChunkResult(0, "First chunk"),
            CreateChunkResult(1, "Second chunk"),
            CreateChunkResult(2, "Third chunk")
        };

        _documentRepository.GetByIdWithChunksAsync(document.Id, Arg.Any<CancellationToken>())
            .Returns(document);
        _chunkingService.ChunkText(document.Content!, document.Id)
            .Returns(chunks);
        _embeddingService.GenerateEmbeddingsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<ReadOnlyMemory<float>>>(CreateEmbeddings(3)));
        _embeddingService.GetVectorDimension().Returns(3);
        _vectorStoreService.UpsertChunksAsync(Arg.Any<IReadOnlyList<(Guid, ReadOnlyMemory<float>, ChunkPayload)>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        await _job.ProcessAsync(document.Id);

        // Assert
        var pointIds = document.Chunks.Select(c => c.QdrantPointId).ToList();
        pointIds.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task ProcessAsync_ChunksHaveCorrectSequentialIndices()
    {
        // Arrange
        var document = CreateTestDocument();
        var chunks = new List<ChunkResult>
        {
            CreateChunkResult(0, "First chunk"),
            CreateChunkResult(1, "Second chunk"),
            CreateChunkResult(2, "Third chunk")
        };

        _documentRepository.GetByIdWithChunksAsync(document.Id, Arg.Any<CancellationToken>())
            .Returns(document);
        _chunkingService.ChunkText(document.Content!, document.Id)
            .Returns(chunks);
        _embeddingService.GenerateEmbeddingsAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<ReadOnlyMemory<float>>>(CreateEmbeddings(3)));
        _embeddingService.GetVectorDimension().Returns(3);
        _vectorStoreService.UpsertChunksAsync(Arg.Any<IReadOnlyList<(Guid, ReadOnlyMemory<float>, ChunkPayload)>>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        await _job.ProcessAsync(document.Id);

        // Assert
        var orderedChunks = document.Chunks.OrderBy(c => c.ChunkIndex).ToList();
        for (int i = 0; i < orderedChunks.Count; i++)
        {
            orderedChunks[i].ChunkIndex.Should().Be(i);
        }
    }
}
