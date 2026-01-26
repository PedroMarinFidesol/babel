using Babel.Application.Common;

namespace Babel.Application.Interfaces;

/// <summary>
/// Servicio para generar embeddings vectoriales de texto.
/// Abstracción sobre Semantic Kernel que permite usar diferentes proveedores.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// Genera un embedding vectorial para un texto.
    /// </summary>
    /// <param name="text">Texto a vectorizar</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Vector de embedding</returns>
    Task<Result<ReadOnlyMemory<float>>> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Genera embeddings vectoriales para múltiples textos en batch.
    /// Más eficiente que llamar GenerateEmbeddingAsync múltiples veces.
    /// </summary>
    /// <param name="texts">Lista de textos a vectorizar</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Lista de vectores de embedding en el mismo orden</returns>
    Task<Result<IReadOnlyList<ReadOnlyMemory<float>>>> GenerateEmbeddingsAsync(
        IReadOnlyList<string> texts,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtiene la dimensión de los vectores generados.
    /// Depende del modelo de embedding configurado.
    /// </summary>
    /// <returns>Dimensión del vector (ej: 768 para nomic-embed-text, 1536 para ada-002)</returns>
    int GetVectorDimension();
}
