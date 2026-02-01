using Babel.Application.Chat.Queries;
using Babel.Application.DTOs;
using Babel.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Babel.WebUI.Controllers;

/// <summary>
/// Controlador REST para el chat RAG con documentos del proyecto.
/// </summary>
[ApiController]
[Route("api/projects/{projectId:guid}")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IMediator mediator,
        IChatService chatService,
        ILogger<ChatController> logger)
    {
        _mediator = mediator;
        _chatService = chatService;
        _logger = logger;
    }

    /// <summary>
    /// Envía un mensaje al chat RAG y recibe respuesta completa.
    /// </summary>
    [HttpPost("chat")]
    [ProducesResponseType(typeof(ChatResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Chat(
        Guid projectId,
        [FromBody] ChatRequestDto request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Chat request for project {ProjectId}", projectId);

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { error = "El mensaje no puede estar vacío." });
        }

        var query = new ChatQuery(projectId, request.Message);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Chat failed for project {ProjectId}: {Error}",
                projectId, result.Error.Description);

            return result.Error.Code switch
            {
                "Chat.ProjectNotFound" => NotFound(new { error = result.Error.Description }),
                "Chat.NoDocuments" => BadRequest(new { error = result.Error.Description }),
                _ => BadRequest(new { error = result.Error.Description })
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Envía un mensaje al chat RAG con respuesta en streaming (SSE).
    /// </summary>
    [HttpPost("chat/stream")]
    [Produces("text/event-stream")]
    public async Task ChatStream(
        Guid projectId,
        [FromBody] ChatRequestDto request,
        CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        _logger.LogInformation("Chat stream request for project {ProjectId}", projectId);

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            await WriteSSEAsync("error", "El mensaje no puede estar vacío.");
            return;
        }

        try
        {
            List<DocumentReferenceDto> references = [];

            await foreach (var item in _chatService.ChatStreamWithReferencesAsync(
                projectId, request.Message, cancellationToken))
            {
                if (item.Item1 is string token)
                {
                    await WriteSSEAsync("token", token);
                }
                else if (item.Item1 is object marker && marker.GetType().GetProperty("__references") != null)
                {
                    references = item.Item2;
                }
            }

            await WriteSSEAsync("done", "");

            if (references.Count > 0)
            {
                var refsJson = System.Text.Json.JsonSerializer.Serialize(references,
                    new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
                await WriteSSEAsync("references", refsJson);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Chat stream cancelled for project {ProjectId}", projectId);
            await WriteSSEAsync("cancelled", "");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chat stream for project {ProjectId}", projectId);
            await WriteSSEAsync("error", ex.Message);
        }
    }

    private async Task WriteSSEAsync(string eventType, string data)
    {
        var escapedData = data.Replace("\n", "\\n").Replace("\r", "");
        var message = $"event: {eventType}\ndata: {escapedData}\n\n";
        await Response.WriteAsync(message);
        await Response.Body.FlushAsync();
    }
}
