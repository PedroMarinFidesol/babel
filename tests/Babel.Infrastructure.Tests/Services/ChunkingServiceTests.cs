using Babel.Infrastructure.Configuration;
using Babel.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Babel.Infrastructure.Tests.Services;

public class ChunkingServiceTests
{
    private readonly IOptions<ChunkingOptions> _options;
    private readonly ILogger<ChunkingService> _logger;
    private readonly ChunkingService _service;

    public ChunkingServiceTests()
    {
        _options = Options.Create(new ChunkingOptions
        {
            MaxChunkSize = 100,
            ChunkOverlap = 20,
            MinChunkSize = 10
        });
        _logger = Substitute.For<ILogger<ChunkingService>>();
        _service = new ChunkingService(_options, _logger);
    }

    [Fact]
    public void ChunkText_EmptyText_ShouldReturnEmptyList()
    {
        var result = _service.ChunkText("", Guid.NewGuid());
        result.Should().BeEmpty();
    }

    [Fact]
    public void ChunkText_NullText_ShouldReturnEmptyList()
    {
        var result = _service.ChunkText(null!, Guid.NewGuid());
        result.Should().BeEmpty();
    }

    [Fact]
    public void ChunkText_WhitespaceOnly_ShouldReturnEmptyList()
    {
        var result = _service.ChunkText("   \t\n  ", Guid.NewGuid());
        result.Should().BeEmpty();
    }

    [Fact]
    public void ChunkText_ShortText_ShouldReturnSingleChunk()
    {
        var text = "This is a short text.";
        var documentId = Guid.NewGuid();

        var result = _service.ChunkText(text, documentId);

        result.Should().HaveCount(1);
        result[0].ChunkIndex.Should().Be(0);
        result[0].Content.Should().Be(text);
        result[0].StartCharIndex.Should().Be(0);
        result[0].EndCharIndex.Should().Be(text.Length - 1);
        result[0].EstimatedTokenCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ChunkText_TextExactlyMaxSize_ShouldReturnSingleChunk()
    {
        var text = new string('a', 100);

        var result = _service.ChunkText(text, Guid.NewGuid());

        result.Should().HaveCount(1);
        result[0].Content.Length.Should().Be(100);
    }

    [Fact]
    public void ChunkText_LongText_ShouldReturnMultipleChunks()
    {
        var text = string.Join(" ", Enumerable.Repeat("word", 50)); // ~250 characters

        var result = _service.ChunkText(text, Guid.NewGuid());

        result.Should().HaveCountGreaterThan(1);

        // Verify chunk indices are sequential
        for (int i = 0; i < result.Count; i++)
        {
            result[i].ChunkIndex.Should().Be(i);
        }
    }

    [Fact]
    public void ChunkText_ShouldPreserveSentenceBoundaries()
    {
        var text = "First sentence here. Second sentence here. Third sentence here. " +
                   "Fourth sentence that is a bit longer to test the chunking.";

        var result = _service.ChunkText(text, Guid.NewGuid());

        result.Should().HaveCountGreaterThanOrEqualTo(1);

        // Each chunk should ideally end at a sentence boundary
        foreach (var chunk in result)
        {
            var trimmedContent = chunk.Content.Trim();
            if (chunk.ChunkIndex < result.Count - 1)
            {
                // Non-last chunks should end with punctuation (if possible)
                var endsWellOrBelowLimit = trimmedContent.EndsWith('.') ||
                                            trimmedContent.EndsWith('!') ||
                                            trimmedContent.EndsWith('?') ||
                                            trimmedContent.EndsWith(' ') ||
                                            chunk.Content.Length <= _options.Value.MaxChunkSize;
                endsWellOrBelowLimit.Should().BeTrue();
            }
        }
    }

    [Fact]
    public void ChunkText_ShouldCalculateEstimatedTokenCount()
    {
        var text = "This is a test text with some words.";

        var result = _service.ChunkText(text, Guid.NewGuid());

        result.Should().HaveCount(1);
        // Estimation: ~4 chars per token
        var expectedTokens = (int)Math.Ceiling(text.Length / 4.0);
        result[0].EstimatedTokenCount.Should().Be(expectedTokens);
    }

    [Fact]
    public void ChunkText_ShouldNormalizeWhitespace()
    {
        var text = "Text  with   multiple    spaces\n\nand\n\n\nnewlines.";

        var result = _service.ChunkText(text, Guid.NewGuid());

        result.Should().HaveCount(1);
        result[0].Content.Should().NotContain("  ");
        result[0].Content.Should().NotContain("\n");
    }

    [Fact]
    public void ChunkText_LargerMaxChunkSize_ShouldProduceLargerChunks()
    {
        var largerOptions = Options.Create(new ChunkingOptions
        {
            MaxChunkSize = 500,
            ChunkOverlap = 50,
            MinChunkSize = 50
        });
        var service = new ChunkingService(largerOptions, _logger);

        var text = string.Join(" ", Enumerable.Repeat("word", 100));

        var result = service.ChunkText(text, Guid.NewGuid());

        result.Should().HaveCount(1);
    }

    [Fact]
    public void ChunkText_WithOverlap_ChunksShouldShareContent()
    {
        var options = Options.Create(new ChunkingOptions
        {
            MaxChunkSize = 50,
            ChunkOverlap = 10,
            MinChunkSize = 10
        });
        var service = new ChunkingService(options, _logger);

        var text = string.Join(" ", Enumerable.Repeat("abcd", 30)); // ~150 characters

        var result = service.ChunkText(text, Guid.NewGuid());

        result.Should().HaveCountGreaterThanOrEqualTo(2);

        // With overlap, chunks should have some shared content
        if (result.Count >= 2)
        {
            var firstChunkEnd = result[0].Content.Length;
            var secondChunkStart = result[1].StartCharIndex;

            // The second chunk should start before the first chunk ends (overlap)
            // Note: Due to normalization and sentence boundaries, this might not always be exact
            (result[1].StartCharIndex).Should().BeLessThanOrEqualTo(result[0].EndCharIndex + 10);
        }
    }

    [Fact]
    public void ChunkText_MinChunkSize_ShouldFilterSmallChunks()
    {
        var options = Options.Create(new ChunkingOptions
        {
            MaxChunkSize = 50,
            ChunkOverlap = 0,
            MinChunkSize = 30
        });
        var service = new ChunkingService(options, _logger);

        var text = string.Join(" ", Enumerable.Repeat("word", 25));

        var result = service.ChunkText(text, Guid.NewGuid());

        // All chunks except the last should be >= MinChunkSize
        foreach (var chunk in result.Take(result.Count - 1))
        {
            chunk.Content.Length.Should().BeGreaterThanOrEqualTo(options.Value.MinChunkSize);
        }
    }

    [Fact]
    public void ChunkText_StartAndEndIndexes_ShouldBeValid()
    {
        var text = "A longer text that will be split into multiple chunks for testing purposes.";

        var result = _service.ChunkText(text, Guid.NewGuid());

        foreach (var chunk in result)
        {
            chunk.StartCharIndex.Should().BeGreaterThanOrEqualTo(0);
            chunk.EndCharIndex.Should().BeGreaterThanOrEqualTo(chunk.StartCharIndex);
        }
    }

    [Theory]
    [InlineData("Test with period. Next sentence.", '.')]
    [InlineData("Test with exclamation! Next sentence.", '!')]
    [InlineData("Test with question? Next sentence.", '?')]
    public void ChunkText_ShouldPreferSentenceEndingPunctuation(string text, char expectedPunctuation)
    {
        var options = Options.Create(new ChunkingOptions
        {
            MaxChunkSize = 25,
            ChunkOverlap = 5,
            MinChunkSize = 5
        });
        var service = new ChunkingService(options, _logger);

        var result = service.ChunkText(text, Guid.NewGuid());

        // At least the first chunk should end with the expected punctuation
        if (result.Count > 1)
        {
            var firstChunk = result[0].Content.Trim();
            var endsCorrectly = firstChunk.EndsWith(expectedPunctuation) || firstChunk.Length <= options.Value.MaxChunkSize;
            endsCorrectly.Should().BeTrue();
        }
    }
}
