using Babel.Application.Common;
using Babel.Infrastructure.Configuration;
using Babel.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

#pragma warning disable SKEXP0001 // ITextEmbeddingGenerationService is experimental

namespace Babel.Infrastructure.Tests.Services;

public class SemanticKernelEmbeddingServiceTests
{
    private readonly IOptions<QdrantOptions> _qdrantOptions;
    private readonly ILogger<SemanticKernelEmbeddingService> _logger;

    public SemanticKernelEmbeddingServiceTests()
    {
        _qdrantOptions = Options.Create(new QdrantOptions
        {
            VectorSize = 1536,
            CollectionName = "test_collection",
            Host = "localhost",
            GrpcPort = 6334
        });
        _logger = Substitute.For<ILogger<SemanticKernelEmbeddingService>>();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullKernel_ShouldNotThrow()
    {
        // Act
        var service = new SemanticKernelEmbeddingService(_qdrantOptions, _logger, kernel: null);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void GetVectorDimension_ShouldReturnConfiguredVectorSize()
    {
        // Arrange
        var service = new SemanticKernelEmbeddingService(_qdrantOptions, _logger, kernel: null);

        // Act
        var dimension = service.GetVectorDimension();

        // Assert
        dimension.Should().Be(1536);
    }

    [Fact]
    public void GetVectorDimension_WithDifferentConfig_ShouldReturnCorrectValue()
    {
        // Arrange
        var options = Options.Create(new QdrantOptions { VectorSize = 768 });
        var service = new SemanticKernelEmbeddingService(options, _logger, kernel: null);

        // Act
        var dimension = service.GetVectorDimension();

        // Assert
        dimension.Should().Be(768);
    }

    #endregion

    #region GenerateEmbeddingAsync Tests

    [Fact]
    public async Task GenerateEmbeddingAsync_EmptyText_ShouldReturnEmptyContentError()
    {
        // Arrange
        var service = new SemanticKernelEmbeddingService(_qdrantOptions, _logger, kernel: null);

        // Act
        var result = await service.GenerateEmbeddingAsync("");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(DomainErrors.Vectorization.EmptyContent.Code);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_NullText_ShouldReturnEmptyContentError()
    {
        // Arrange
        var service = new SemanticKernelEmbeddingService(_qdrantOptions, _logger, kernel: null);

        // Act
        var result = await service.GenerateEmbeddingAsync(null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(DomainErrors.Vectorization.EmptyContent.Code);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_WhitespaceText_ShouldReturnEmptyContentError()
    {
        // Arrange
        var service = new SemanticKernelEmbeddingService(_qdrantOptions, _logger, kernel: null);

        // Act
        var result = await service.GenerateEmbeddingAsync("   \t\n  ");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(DomainErrors.Vectorization.EmptyContent.Code);
    }

    [Fact]
    public async Task GenerateEmbeddingAsync_NoKernel_ShouldReturnProviderNotConfiguredError()
    {
        // Arrange
        var service = new SemanticKernelEmbeddingService(_qdrantOptions, _logger, kernel: null);

        // Act
        var result = await service.GenerateEmbeddingAsync("Valid text");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(DomainErrors.Vectorization.ProviderNotConfigured.Code);
    }

    #endregion

    #region GenerateEmbeddingsAsync (Batch) Tests

    [Fact]
    public async Task GenerateEmbeddingsAsync_NullList_ShouldReturnEmptyContentError()
    {
        // Arrange
        var service = new SemanticKernelEmbeddingService(_qdrantOptions, _logger, kernel: null);

        // Act
        var result = await service.GenerateEmbeddingsAsync(null!);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(DomainErrors.Vectorization.EmptyContent.Code);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_EmptyList_ShouldReturnEmptyContentError()
    {
        // Arrange
        var service = new SemanticKernelEmbeddingService(_qdrantOptions, _logger, kernel: null);

        // Act
        var result = await service.GenerateEmbeddingsAsync(new List<string>());

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(DomainErrors.Vectorization.EmptyContent.Code);
    }

    [Fact]
    public async Task GenerateEmbeddingsAsync_NoKernel_ShouldReturnProviderNotConfiguredError()
    {
        // Arrange
        var service = new SemanticKernelEmbeddingService(_qdrantOptions, _logger, kernel: null);

        // Act
        var result = await service.GenerateEmbeddingsAsync(new List<string> { "Text 1", "Text 2" });

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(DomainErrors.Vectorization.ProviderNotConfigured.Code);
    }

    #endregion

    #region QdrantOptions Validation Tests

    [Fact]
    public void QdrantOptions_DefaultValues_ShouldBeValid()
    {
        // Arrange
        var options = new QdrantOptions();

        // Assert
        options.VectorSize.Should().Be(1536);
        options.CollectionName.Should().Be("babel_documents");
        options.Host.Should().Be("localhost");
        options.GrpcPort.Should().Be(6334);
        options.HttpPort.Should().Be(6333);
    }

    [Theory]
    [InlineData(384)]   // all-minilm
    [InlineData(768)]   // nomic-embed-text
    [InlineData(1536)]  // OpenAI ada-002
    [InlineData(3072)]  // OpenAI embedding-3-large
    public void GetVectorDimension_ShouldSupportVariousDimensions(int vectorSize)
    {
        // Arrange
        var options = Options.Create(new QdrantOptions { VectorSize = vectorSize });
        var service = new SemanticKernelEmbeddingService(options, _logger, kernel: null);

        // Act
        var dimension = service.GetVectorDimension();

        // Assert
        dimension.Should().Be(vectorSize);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GenerateEmbeddingAsync_ValidTextButNoProvider_ShouldLogWarning()
    {
        // Arrange
        var service = new SemanticKernelEmbeddingService(_qdrantOptions, _logger, kernel: null);

        // Act
        await service.GenerateEmbeddingAsync("Test text");

        // Assert - Verify logging occurred (using NSubstitute's Received)
        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("no está configurado")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion
}

#region DomainErrors.Vectorization Reference Tests

public class VectorizationErrorsTests
{
    [Fact]
    public void EmptyContent_ShouldHaveCorrectCode()
    {
        var error = DomainErrors.Vectorization.EmptyContent;
        error.Code.Should().Be("Vectorization.EmptyContent");
        error.Description.Should().NotBeEmpty();
    }

    [Fact]
    public void ProviderNotConfigured_ShouldHaveCorrectCode()
    {
        var error = DomainErrors.Vectorization.ProviderNotConfigured;
        error.Code.Should().Be("Vectorization.ProviderNotConfigured");
        error.Description.Should().NotBeEmpty();
    }

    [Fact]
    public void EmbeddingFailed_ShouldHaveCorrectCode()
    {
        var error = DomainErrors.Vectorization.EmbeddingFailed;
        error.Code.Should().Be("Vectorization.EmbeddingFailed");
        error.Description.Should().NotBeEmpty();
    }

    [Fact]
    public void QdrantOperationFailed_ShouldHaveCorrectCode()
    {
        var error = DomainErrors.Vectorization.QdrantOperationFailed;
        error.Code.Should().Be("Vectorization.QdrantOperationFailed");
        error.Description.Should().NotBeEmpty();
    }

    [Fact]
    public void ChunkingFailed_ShouldHaveCorrectCode()
    {
        var error = DomainErrors.Vectorization.ChunkingFailed;
        error.Code.Should().Be("Vectorization.ChunkingFailed");
        error.Description.Should().NotBeEmpty();
    }

    [Fact]
    public void DocumentNotReady_ShouldHaveCorrectCode()
    {
        var error = DomainErrors.Vectorization.DocumentNotReady;
        error.Code.Should().Be("Vectorization.DocumentNotReady");
        error.Description.Should().NotBeEmpty();
    }
}

#endregion
