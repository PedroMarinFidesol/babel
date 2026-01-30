using Babel.Application.Common;
using Babel.Application.DTOs;

namespace Babel.Application.Interfaces;

/// <summary>
/// Servicio de chat con patrón RAG (Retrieval Augmented Generation).
/// Permite hacer preguntas sobre los documentos de un proyecto y obtener
/// respuestas con referencias a los documentos fuente.
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Procesa una pregunta del usuario usando RAG.
    /// Busca documentos relevantes, construye contexto y genera respuesta.
    /// </summary>
    /// <param name="projectId">ID del proyecto para limitar la búsqueda</param>
    /// <param name="message">Pregunta del usuario</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Respuesta con referencias a documentos fuente</returns>
    Task<Result<ChatResponseDto>> ChatAsync(
        Guid projectId,
        string message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Procesa una pregunta del usuario usando RAG con streaming de respuesta.
    /// Útil para mostrar la respuesta progresivamente en la UI.
    /// </summary>
    /// <param name="projectId">ID del proyecto para limitar la búsqueda</param>
    /// <param name="message">Pregunta del usuario</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Stream de tokens de respuesta</returns>
    IAsyncEnumerable<string> ChatStreamAsync(
        Guid projectId,
        string message,
        CancellationToken cancellationToken = default);
}
