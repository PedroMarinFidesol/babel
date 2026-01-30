using Babel.Application.Common;
using Babel.Application.Interfaces;
using Babel.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;

#pragma warning disable SKEXP0001 // ITextEmbeddingGenerationService is experimental

namespace Babel.Infrastructure.Services;

/// <summary>
/// Servicio de embeddings usando Semantic Kernel.
/// Soporta múltiples proveedores (Ollama, OpenAI) según configuración.
/// </summary>
public class SemanticKernelEmbeddingService : IEmbeddingService
{
    private readonly Kernel? _kernel;
    private readonly QdrantOptions _qdrantOptions;
    private readonly ILogger<SemanticKernelEmbeddingService> _logger;

    public SemanticKernelEmbeddingService(
        IOptions<QdrantOptions> qdrantOptions,
        ILogger<SemanticKernelEmbeddingService> logger,
        Kernel? kernel = null)
    {
        _qdrantOptions = qdrantOptions.Value;
        _logger = logger;
        _kernel = kernel;
    }

    /// <inheritdoc />
    public async Task<Result<ReadOnlyMemory<float>>> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Result.Failure<ReadOnlyMemory<float>>(DomainErrors.Vectorization.EmptyContent);
        }

        var embeddingService = GetEmbeddingService();
        if (embeddingService is null)
        {
            _logger.LogWarning("Embedding generator no está configurado");
            return Result.Failure<ReadOnlyMemory<float>>(DomainErrors.Vectorization.ProviderNotConfigured);
        }

        try
        {
            var embeddings = await embeddingService.GenerateEmbeddingsAsync(
                new[] { text },
                kernel: _kernel,
                cancellationToken: cancellationToken);

            if (embeddings is null || embeddings.Count == 0)
            {
                _logger.LogError("El generador de embeddings retornó null o vacío");
                return Result.Failure<ReadOnlyMemory<float>>(DomainErrors.Vectorization.EmbeddingFailed);
            }

            var vector = embeddings[0];
            _logger.LogDebug(
                "Embedding generado exitosamente. Dimensiones: {Dimensions}",
                vector.Length);

            return Result.Success(vector);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando embedding para texto de {Length} caracteres", text.Length);
            return Result.Failure<ReadOnlyMemory<float>>(
                new Error("Vectorization.EmbeddingFailed", $"Error generando embedding: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ReadOnlyMemory<float>>>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default)
    {
        if (texts is null || texts.Count == 0)
        {
            return Result.Failure<IReadOnlyList<ReadOnlyMemory<float>>>(DomainErrors.Vectorization.EmptyContent);
        }

        var embeddingService = GetEmbeddingService();
        if (embeddingService is null)
        {
            _logger.LogWarning("Embedding generator no está configurado");
            return Result.Failure<IReadOnlyList<ReadOnlyMemory<float>>>(DomainErrors.Vectorization.ProviderNotConfigured);
        }

        try
        {
            var embeddings = await embeddingService.GenerateEmbeddingsAsync(
                texts.ToList(),
                kernel: _kernel,
                cancellationToken: cancellationToken);

            if (embeddings is null || embeddings.Count != texts.Count)
            {
                _logger.LogError(
                    "El generador de embeddings retornó resultados inválidos. Esperados: {Expected}, Recibidos: {Received}",
                    texts.Count, embeddings?.Count ?? 0);
                return Result.Failure<IReadOnlyList<ReadOnlyMemory<float>>>(DomainErrors.Vectorization.EmbeddingFailed);
            }

            _logger.LogDebug(
                "Embeddings generados exitosamente. Cantidad: {Count}, Dimensiones: {Dimensions}",
                embeddings.Count, embeddings.FirstOrDefault().Length);

            return Result.Success<IReadOnlyList<ReadOnlyMemory<float>>>(embeddings.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generando embeddings en batch para {Count} textos", texts.Count);
            return Result.Failure<IReadOnlyList<ReadOnlyMemory<float>>>(
                new Error("Vectorization.EmbeddingFailed", $"Error generando embeddings: {ex.Message}"));
        }
    }

    /// <inheritdoc />
    public int GetVectorDimension()
    {
        return _qdrantOptions.VectorSize;
    }

    private ITextEmbeddingGenerationService? GetEmbeddingService()
    {
        if (_kernel is null)
        {
            _logger.LogWarning("Kernel no está configurado");
            return null;
        }

        try
        {
            var service = _kernel.GetRequiredService<ITextEmbeddingGenerationService>();
            return service;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo resolver ITextEmbeddingGenerationService del Kernel");
            return null;
        }
    }
}
