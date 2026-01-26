namespace Babel.Application.Interfaces;

/// <summary>
/// Resultado de dividir texto en un chunk.
/// </summary>
/// <param name="ChunkIndex">Índice del chunk dentro del documento (0, 1, 2...)</param>
/// <param name="Content">Texto del chunk</param>
/// <param name="StartCharIndex">Posición de inicio en el texto original</param>
/// <param name="EndCharIndex">Posición de fin en el texto original</param>
/// <param name="EstimatedTokenCount">Número estimado de tokens</param>
public record ChunkResult(
    int ChunkIndex,
    string Content,
    int StartCharIndex,
    int EndCharIndex,
    int EstimatedTokenCount);

/// <summary>
/// Servicio para dividir texto en chunks para vectorización.
/// Usa overlap para mantener contexto entre chunks.
/// </summary>
public interface IChunkingService
{
    /// <summary>
    /// Divide el texto de un documento en chunks.
    /// </summary>
    /// <param name="text">Texto a dividir</param>
    /// <param name="documentId">ID del documento (para logging)</param>
    /// <returns>Lista de chunks con sus metadatos</returns>
    IReadOnlyList<ChunkResult> ChunkText(string text, Guid documentId);
}
