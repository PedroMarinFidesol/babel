using Babel.Application.Interfaces;
using Babel.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Babel.Infrastructure.Services;

/// <summary>
/// Servicio para dividir texto en chunks para vectorización.
/// Usa overlap configurable para mantener contexto entre chunks.
/// </summary>
public class ChunkingService : IChunkingService
{
    private readonly ChunkingOptions _options;
    private readonly ILogger<ChunkingService> _logger;

    public ChunkingService(
        IOptions<ChunkingOptions> options,
        ILogger<ChunkingService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public IReadOnlyList<ChunkResult> ChunkText(string text, Guid documentId)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Texto vacío recibido para chunking. DocumentId: {DocumentId}", documentId);
            return [];
        }

        var chunks = new List<ChunkResult>();
        var normalizedText = NormalizeText(text);

        if (normalizedText.Length <= _options.MaxChunkSize)
        {
            // El texto cabe en un solo chunk
            chunks.Add(CreateChunk(normalizedText, 0, 0, normalizedText.Length - 1));
            _logger.LogDebug(
                "Documento {DocumentId} procesado como chunk único. Tamaño: {Size} caracteres",
                documentId, normalizedText.Length);
            return chunks;
        }

        // Dividir en chunks con overlap
        int currentPosition = 0;
        int chunkIndex = 0;

        while (currentPosition < normalizedText.Length)
        {
            int endPosition = Math.Min(currentPosition + _options.MaxChunkSize, normalizedText.Length);

            // Si no estamos al final, intentar cortar en un límite natural
            if (endPosition < normalizedText.Length)
            {
                endPosition = FindBestBreakPoint(normalizedText, currentPosition, endPosition);
            }

            // Asegurar que endPosition siempre avanza al menos un carácter
            if (endPosition <= currentPosition)
            {
                endPosition = Math.Min(currentPosition + _options.MaxChunkSize, normalizedText.Length);
            }

            string chunkContent = normalizedText[currentPosition..endPosition];

            // Solo incluir chunks que superen el tamaño mínimo (excepto el último)
            if (chunkContent.Length >= _options.MinChunkSize ||
                endPosition >= normalizedText.Length)
            {
                chunks.Add(CreateChunk(chunkContent, chunkIndex, currentPosition, endPosition - 1));
                chunkIndex++;
            }

            // Calcular siguiente posición considerando overlap
            int nextPosition = endPosition - _options.ChunkOverlap;

            // Asegurar que siempre avanzamos al menos un carácter
            if (nextPosition <= currentPosition)
            {
                nextPosition = endPosition;
            }

            currentPosition = nextPosition;
        }

        _logger.LogDebug(
            "Documento {DocumentId} dividido en {ChunkCount} chunks. Tamaño original: {Size} caracteres",
            documentId, chunks.Count, text.Length);

        return chunks;
    }

    /// <summary>
    /// Normaliza el texto preservando saltos de línea para mantener la estructura.
    /// </summary>
    private static string NormalizeText(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        // 1. Unificar saltos de línea
        text = text.Replace("\r\n", "\n").Replace("\r", "\n");
        
        // 2. Reducir múltiples espacios horizontales (tabuladores, espacios) a uno solo
        text = System.Text.RegularExpressions.Regex.Replace(text, @"[ \t]+", " ");
        
        // 3. Reducir múltiples saltos de línea (3+) a dos (párrafo)
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\n{3,}", "\n\n");
        
        return text.Trim();
    }

    /// <summary>
    /// Busca el mejor punto de corte cerca de endPosition.
    /// Prioriza: fin de párrafo > fin de oración > espacio.
    /// </summary>
    private int FindBestBreakPoint(string text, int startPosition, int maxEndPosition)
    {
        int searchStart = Math.Max(startPosition, maxEndPosition - 200);

        // Buscar fin de párrafo (doble newline) - ya normalizado a espacios
        // Buscar fin de oración (. ! ?)
        for (int i = maxEndPosition - 1; i >= searchStart; i--)
        {
            char c = text[i];
            if (c == '.' || c == '!' || c == '?')
            {
                // Verificar que el siguiente carácter (si existe) sea espacio
                if (i + 1 < text.Length && char.IsWhiteSpace(text[i + 1]))
                {
                    return i + 1;
                }
            }
        }

        // Buscar espacio
        for (int i = maxEndPosition - 1; i >= searchStart; i--)
        {
            if (char.IsWhiteSpace(text[i]))
            {
                return i;
            }
        }

        // No se encontró buen punto de corte, usar maxEndPosition
        return maxEndPosition;
    }

    /// <summary>
    /// Crea un ChunkResult con estimación de tokens.
    /// </summary>
    private static ChunkResult CreateChunk(string content, int index, int startChar, int endChar)
    {
        // Estimación simple: ~4 caracteres por token (promedio para texto en español/inglés)
        int estimatedTokens = (int)Math.Ceiling(content.Length / 4.0);

        return new ChunkResult(
            ChunkIndex: index,
            Content: content,
            StartCharIndex: startChar,
            EndCharIndex: endChar,
            EstimatedTokenCount: estimatedTokens);
    }
}
