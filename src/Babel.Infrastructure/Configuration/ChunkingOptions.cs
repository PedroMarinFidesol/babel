using System.ComponentModel.DataAnnotations;

namespace Babel.Infrastructure.Configuration;

/// <summary>
/// Opciones de configuración para el chunking de documentos.
/// </summary>
public class ChunkingOptions
{
    public const string SectionName = "Chunking";

    /// <summary>
    /// Tamaño máximo de cada chunk en caracteres.
    /// </summary>
    [Range(100, 10000)]
    public int MaxChunkSize { get; set; } = 1000;

    /// <summary>
    /// Número de caracteres de solapamiento entre chunks consecutivos.
    /// </summary>
    [Range(0, 1000)]
    public int ChunkOverlap { get; set; } = 200;

    /// <summary>
    /// Tamaño mínimo de un chunk para ser procesado.
    /// </summary>
    [Range(10, 500)]
    public int MinChunkSize { get; set; } = 100;
}
