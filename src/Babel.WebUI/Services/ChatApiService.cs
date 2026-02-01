using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Babel.Application.DTOs;

namespace Babel.WebUI.Services;

/// <summary>
/// Implementación del servicio de comunicación con la API de chat RAG.
/// </summary>
public class ChatApiService : IChatApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ChatApiService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ChatApiService(HttpClient httpClient, ILogger<ChatApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ChatResponseDto?> SendMessageAsync(
        Guid projectId,
        string message,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ChatRequestDto { ProjectId = projectId, Message = message };
            var response = await _httpClient.PostAsJsonAsync(
                $"api/projects/{projectId}/chat",
                request,
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Error en chat API: {StatusCode} - {Error}",
                    response.StatusCode, error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ChatResponseDto>(JsonOptions, cancellationToken);
        }
        catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Chat request cancelled");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error llamando a chat API para proyecto {ProjectId}", projectId);
            return null;
        }
    }

    public async IAsyncEnumerable<ChatStreamEvent> SendMessageStreamAsync(
        Guid projectId,
        string message,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = new ChatRequestDto { ProjectId = projectId, Message = message };
        var jsonContent = new StringContent(
            JsonSerializer.Serialize(request, JsonOptions),
            Encoding.UTF8,
            "application/json");

        HttpResponseMessage? response = null;
        try
        {
            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"api/projects/{projectId}/chat/stream")
            {
                Content = jsonContent
            };

            response = await _httpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Error en chat stream API: {StatusCode} - {Error}",
                    response.StatusCode, error);
                yield return new ChatStreamEvent("error", "Error conectando con el servidor");
                yield break;
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            string? currentEvent = null;

            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken)) != null
                   && !cancellationToken.IsCancellationRequested)
            {

                if (string.IsNullOrEmpty(line))
                {
                    // Línea vacía indica fin de evento SSE
                    continue;
                }

                if (line.StartsWith("event: "))
                {
                    currentEvent = line[7..];
                }
                else if (line.StartsWith("data: ") && currentEvent != null)
                {
                    var data = line[6..];

                    ChatStreamEvent streamEvent;

                    if (currentEvent == "references")
                    {
                        streamEvent = new ChatStreamEvent(currentEvent, string.Empty);
                        try
                        {
                            var refs = System.Text.Json.JsonSerializer.Deserialize<List<DocumentReferenceDto>>(
                                data,
                                new System.Text.Json.JsonSerializerOptions
                                {
                                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                                });
                            streamEvent.References = refs ?? [];
                        }
                        catch
                        {
                            streamEvent.References = [];
                        }
                    }
                    else
                    {
                        data = data.Replace("\\n", "\n");
                        streamEvent = new ChatStreamEvent(currentEvent, data);
                    }

                    yield return streamEvent;

                    if (currentEvent is "done" or "error" or "cancelled")
                    {
                        yield break;
                    }
                }
            }
        }
        finally
        {
            response?.Dispose();
        }
    }
}
