using Babel.Application.Common;
using Babel.Application.Interfaces;
using Babel.Infrastructure.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Babel.Infrastructure.Services;

/// <summary>
/// Servicio de embeddings usando Microsoft.Extensions.AI.
/// Soporta múltiples proveedores (Ollama, OpenAI) según configuración.
/// </summary>
public class SemanticKernelEmbeddingService : IEmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>>? _embeddingGenerator;
    private readonly Microsoft.SemanticKernel.Kernel? _kernel;
    private readonly SemanticKernelOptions _options;
    private readonly QdrantOptions _qdrantOptions;
    private readonly ILogger<SemanticKernelEmbeddingService> _logger;

        public SemanticKernelEmbeddingService(
        IOptions<SemanticKernelOptions> options,
        IOptions<QdrantOptions> qdrantOptions,
        ILogger<SemanticKernelEmbeddingService> logger,
        Microsoft.SemanticKernel.Kernel? kernel = null,
        IEmbeddingGenerator<string, Embedding<float>>? embeddingGenerator = null)
    {
        _options = options.Value;
        _qdrantOptions = qdrantOptions.Value;
        _logger = logger;
        _kernel = kernel;
        _embeddingGenerator = embeddingGenerator;
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

        if (_embeddingGenerator is null)
        {
            // Intentar resolver el generador desde el Kernel si está disponible
            if (_kernel is not null)
            {
                try
                {
                    var svc = _kernel.Services.GetService(typeof(IEmbeddingGenerator<string, Embedding<float>>));
                    if (svc is IEmbeddingGenerator<string, Embedding<float>> gen)
                    {
                        _logger.LogDebug("Resolved embedding generator from Kernel's service provider");
                        // Reasignar campo local para futuras llamadas
                        // Nota: reflection no necesario porque ya comprobamos el tipo
                        // Usar el generador resuelto
                        var embedding = await gen.GenerateAsync(text, cancellationToken: cancellationToken);

                        if (embedding is null)
                        {
                            _logger.LogError("El generador de embeddings retornó null");
                            return Result.Failure<ReadOnlyMemory<float>>(DomainErrors.Vectorization.EmbeddingFailed);
                        }

                        _logger.LogDebug(
                            "Embedding generado exitosamente. Dimensiones: {Dimensions}",
                            embedding.Vector.Length);

                        return Result.Success(embedding.Vector);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error resolviendo IEmbeddingGenerator desde Kernel");
                }
            }

            _logger.LogWarning("Embedding generator no está configurado");
            return Result.Failure<ReadOnlyMemory<float>>(DomainErrors.Vectorization.ProviderNotConfigured);
        }

        try
        {
            var embedding = await _embeddingGenerator.GenerateAsync(
                text,
                cancellationToken: cancellationToken);

            if (embedding is null)
            {
                _logger.LogError("El generador de embeddings retornó null");
                return Result.Failure<ReadOnlyMemory<float>>(DomainErrors.Vectorization.EmbeddingFailed);
            }

            _logger.LogDebug(
                "Embedding generado exitosamente. Dimensiones: {Dimensions}",
                embedding.Vector.Length);

            return Result.Success(embedding.Vector);
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

        if (_embeddingGenerator is null)
        {
            _logger.LogWarning("Embedding generator no está configurado");
            return Result.Failure<IReadOnlyList<ReadOnlyMemory<float>>>(DomainErrors.Vectorization.ProviderNotConfigured);
        }

        try
        {
            var embeddings = await _embeddingGenerator.GenerateAsync(
                texts,
                cancellationToken: cancellationToken);

            if (embeddings is null || embeddings.Count != texts.Count)
            {
                _logger.LogError(
                    "El generador de embeddings retornó resultados inválidos. Esperados: {Expected}, Recibidos: {Received}",
                    texts.Count, embeddings?.Count ?? 0);
                return Result.Failure<IReadOnlyList<ReadOnlyMemory<float>>>(DomainErrors.Vectorization.EmbeddingFailed);
            }

            var vectors = embeddings
                .Select(e => e.Vector)
                .ToList();

            _logger.LogDebug(
                "Embeddings generados exitosamente. Cantidad: {Count}, Dimensiones: {Dimensions}",
                vectors.Count, vectors.FirstOrDefault().Length);

            return Result.Success<IReadOnlyList<ReadOnlyMemory<float>>>(vectors);
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
        // Usar el tamaño configurado en Qdrant, que debe coincidir con el modelo de embedding
        return _qdrantOptions.VectorSize;
    }
}
