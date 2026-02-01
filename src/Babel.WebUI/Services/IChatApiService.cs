using Babel.Application.DTOs;
using System.Text.Json.Serialization;

namespace Babel.WebUI.Services;

/// <summary>
/// Servicio para comunicación con la API de chat RAG.
/// </summary>
public interface IChatApiService
{
    /// <summary>
    /// Envía mensaje y recibe respuesta completa.
    /// </summary>
    /// <param name="projectId">ID del proyecto</param>
    /// <param name="message">Mensaje del usuario</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Respuesta del chat con referencias</returns>
    Task<ChatResponseDto?> SendMessageAsync(
        Guid projectId,
        string message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Envía mensaje y recibe respuesta en streaming.
    /// </summary>
    /// <param name="projectId">ID del proyecto</param>
    /// <param name="message">Mensaje del usuario</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Stream de eventos del chat</returns>
    IAsyncEnumerable<ChatStreamEvent> SendMessageStreamAsync(
        Guid projectId,
        string message,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Evento de streaming del chat (SSE).
/// </summary>
/// <param name="EventType">Tipo de evento: token, done, error, cancelled, references</param>
/// <param name="Data">Datos del evento</param>
public record ChatStreamEvent(string EventType, string Data)
{
    [JsonIgnore]
    public List<DocumentReferenceDto>? References { get; set; }
    
    public ChatStreamEvent WithData(string newData) => this with { Data = newData };
};
