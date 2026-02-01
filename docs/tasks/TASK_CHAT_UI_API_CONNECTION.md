# Tarea: Conexión UI Chat con API REST

**Fecha:** 2026-02-01
**Estado:** Completado
**Prioridad:** Alta
**Fases afectadas:** 9 (completar) y 10

---

## Objetivo

Conectar el componente `ChatWindow.razor` con el servicio de chat RAG a través de endpoints REST en `ChatController`. No usar MediatR directamente desde WebUI, sino consumir la API via `HttpClient`.

---

## Arquitectura

```
┌─────────────────────┐     HTTP/SSE      ┌─────────────────────┐
│   Babel.WebUI       │ ◄───────────────► │    Babel.API        │
│                     │                    │                     │
│  ChatWindow.razor   │                    │  ChatController     │
│  ChatApiService     │                    │    ├─ POST /chat    │
│                     │                    │    └─ POST /stream  │
└─────────────────────┘                    └─────────────────────┘
                                                     │
                                                     ▼
                                           ┌─────────────────────┐
                                           │  MediatR Pipeline   │
                                           │    ChatQuery        │
                                           │    ChatQueryHandler │
                                           └─────────────────────┘
                                                     │
                                                     ▼
                                           ┌─────────────────────┐
                                           │  IChatService       │
                                           │  (SemanticKernel)   │
                                           └─────────────────────┘
```

---

## Tareas

### 1. Crear ChatController en Babel.API

**Archivo:** `src/Babel.API/Controllers/ChatController.cs`

**Endpoints:**

| Método | Ruta | Descripción | Request | Response |
|--------|------|-------------|---------|----------|
| POST | `/api/projects/{projectId}/chat` | Chat simple (respuesta completa) | `ChatRequestDto` | `ChatResponseDto` |
| POST | `/api/projects/{projectId}/chat/stream` | Chat con streaming SSE | `ChatRequestDto` | `text/event-stream` |

**Implementación:**

```csharp
using Babel.Application.Chat.Queries;
using Babel.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Babel.API.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}")]
public class ChatController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IChatService _chatService;

    public ChatController(IMediator mediator, IChatService chatService)
    {
        _mediator = mediator;
        _chatService = chatService;
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
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest("El mensaje no puede estar vacío.");
        }

        var query = new ChatQuery(projectId, request.Message);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "Chat.ProjectNotFound" => NotFound(result.Error.Description),
                _ => BadRequest(result.Error.Description)
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Envía un mensaje al chat RAG con respuesta en streaming (SSE).
    /// </summary>
    [HttpPost("chat/stream")]
    public async Task ChatStream(
        Guid projectId,
        [FromBody] ChatRequestDto request,
        CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            await WriteSSEAsync("error", "El mensaje no puede estar vacío.");
            return;
        }

        try
        {
            await foreach (var token in _chatService.ChatStreamAsync(
                projectId, request.Message, cancellationToken))
            {
                await WriteSSEAsync("token", token);
            }

            await WriteSSEAsync("done", "");
        }
        catch (Exception ex)
        {
            await WriteSSEAsync("error", ex.Message);
        }
    }

    private async Task WriteSSEAsync(string eventType, string data)
    {
        var message = $"event: {eventType}\ndata: {data}\n\n";
        await Response.WriteAsync(message);
        await Response.Body.FlushAsync();
    }
}
```

**Notas:**
- El endpoint `/chat` usa MediatR para validación y logging via pipeline
- El endpoint `/chat/stream` usa `IChatService` directamente para streaming
- SSE (Server-Sent Events) es el estándar para streaming HTTP

---

### 2. Crear ChatApiService en Babel.WebUI

**Archivo:** `src/Babel.WebUI/Services/ChatApiService.cs`

**Propósito:** Encapsular las llamadas HTTP a la API de chat.

```csharp
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Babel.Application.DTOs;

namespace Babel.WebUI.Services;

public interface IChatApiService
{
    /// <summary>
    /// Envía mensaje y recibe respuesta completa.
    /// </summary>
    Task<ChatResponseDto?> SendMessageAsync(
        Guid projectId,
        string message,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Envía mensaje y recibe respuesta en streaming.
    /// </summary>
    IAsyncEnumerable<ChatStreamEvent> SendMessageStreamAsync(
        Guid projectId,
        string message,
        CancellationToken cancellationToken = default);
}

public record ChatStreamEvent(string EventType, string Data);

public class ChatApiService : IChatApiService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ChatApiService> _logger;

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
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Error en chat API: {StatusCode} - {Error}",
                    response.StatusCode, error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ChatResponseDto>(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error llamando a chat API");
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
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/projects/{projectId}/chat/stream")
        {
            Content = jsonContent
        };

        using var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            yield return new ChatStreamEvent("error", "Error conectando con el servidor");
            yield break;
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? currentEvent = null;

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);

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
                yield return new ChatStreamEvent(currentEvent, data);

                if (currentEvent == "done" || currentEvent == "error")
                {
                    yield break;
                }
            }
        }
    }
}
```

---

### 3. Registrar HttpClient en WebUI

**Archivo:** `src/Babel.WebUI/Program.cs`

Agregar configuración de HttpClient para el servicio de chat:

```csharp
// Después de builder.Services.AddMudServices()

// Configurar HttpClient para API
builder.Services.AddHttpClient<IChatApiService, ChatApiService>(client =>
{
    // En desarrollo, la API está en el mismo host
    // En producción, configurar via appsettings
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]
        ?? "https://localhost:5001/");
    client.Timeout = TimeSpan.FromMinutes(5); // Timeout largo para streaming
});
```

**Archivo:** `src/Babel.WebUI/appsettings.json`

Agregar configuración:

```json
{
  "ApiBaseUrl": "https://localhost:5001/"
}
```

**Nota:** Si WebUI y API corren en el mismo proceso (como parece ser el caso actual), se puede usar una URL relativa o localhost.

---

### 4. Modificar ChatWindow.razor

**Archivo:** `src/Babel.WebUI/Components/Chat/ChatWindow.razor`

**Cambios principales:**

1. Inyectar `IChatApiService` en lugar de usar placeholder
2. Implementar modo simple (respuesta completa) primero
3. Agregar modo streaming opcional
4. Manejar referencias de documentos
5. Mejorar indicador de "procesando"

```razor
@inject IJSRuntime JS
@inject IChatApiService ChatApi
@inject ISnackbar Snackbar

<MudPaper Elevation="2" Class="d-flex flex-column" Style="height: 500px;">
    <MudToolBar Dense="true" Class="px-4">
        <MudIcon Icon="@Icons.Material.Filled.Chat" Class="mr-2" />
        <MudText Typo="Typo.subtitle1">Chat RAG</MudText>
        <MudSpacer />
        @* Toggle para streaming *@
        <MudTooltip Text="@(_useStreaming ? "Streaming activado" : "Streaming desactivado")">
            <MudToggleIconButton @bind-Toggled="_useStreaming"
                                 Icon="@Icons.Material.Filled.Stream"
                                 ToggledIcon="@Icons.Material.Filled.Stream"
                                 Size="Size.Small"
                                 Color="@(_useStreaming ? Color.Primary : Color.Default)" />
        </MudTooltip>
        <MudTooltip Text="Limpiar conversacion">
            <MudIconButton Icon="@Icons.Material.Filled.DeleteSweep"
                           Size="Size.Small"
                           OnClick="@ClearChat"
                           Disabled="@(_messages.Count == 0)" />
        </MudTooltip>
    </MudToolBar>

    <MudDivider />

    <div @ref="_chatContainer" class="flex-grow-1 overflow-auto pa-4"
         style="background-color: var(--mud-palette-background-grey);">
        @if (_messages.Count == 0)
        {
            <div class="d-flex flex-column align-center justify-center" style="height: 100%;">
                <MudIcon Icon="@Icons.Material.Filled.QuestionAnswer"
                         Size="Size.Large" Color="Color.Secondary" Class="mb-2" />
                <MudText Typo="Typo.body1" Color="Color.Secondary">No hay mensajes</MudText>
                <MudText Typo="Typo.body2" Color="Color.Secondary">
                    Escribe una pregunta sobre los documentos del proyecto
                </MudText>
            </div>
        }
        else
        {
            @foreach (var message in _messages)
            {
                <ChatMessage Message="@message" OnReferenceClick="@HandleReferenceClick" />
            }

            @if (_isProcessing)
            {
                <div class="d-flex align-center pa-2 mb-2">
                    <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2" />
                    <MudText Typo="Typo.body2" Color="Color.Secondary">
                        @if (_useStreaming && !string.IsNullOrEmpty(_streamingContent))
                        {
                            @_streamingContent
                        }
                        else
                        {
                            @("Analizando documentos...")
                        }
                    </MudText>
                </div>
            }
        }
    </div>

    <MudDivider />

    <div class="pa-3">
        <MudTextField @bind-Value="_currentMessage"
                      Placeholder="Escribe tu pregunta..."
                      Variant="Variant.Outlined"
                      Adornment="Adornment.End"
                      AdornmentIcon="@Icons.Material.Filled.Send"
                      AdornmentColor="Color.Primary"
                      OnAdornmentClick="@SendMessageAsync"
                      OnKeyUp="@HandleKeyUp"
                      Disabled="@_isProcessing"
                      Immediate="true"
                      FullWidth="true"
                      Margin="Margin.None" />
    </div>
</MudPaper>

@code {
    [Parameter]
    public Guid ProjectId { get; set; }

    [Parameter]
    public EventCallback<Guid> OnDocumentReferenceClick { get; set; }

    private List<ChatMessageDto> _messages = [];
    private string _currentMessage = "";
    private bool _isProcessing;
    private bool _useStreaming = true;
    private string _streamingContent = "";
    private ElementReference _chatContainer;
    private CancellationTokenSource? _cts;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_messages.Count > 0)
        {
            await ScrollToBottomAsync();
        }
    }

    private async Task HandleKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey && !string.IsNullOrWhiteSpace(_currentMessage))
        {
            await SendMessageAsync();
        }
    }

    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(_currentMessage) || _isProcessing) return;

        var userMessage = _currentMessage.Trim();
        _currentMessage = "";
        _isProcessing = true;
        _streamingContent = "";
        _cts = new CancellationTokenSource();

        // Agregar mensaje del usuario inmediatamente
        _messages.Add(new ChatMessageDto
        {
            Content = userMessage,
            IsUser = true,
            Timestamp = DateTime.UtcNow
        });

        StateHasChanged();
        await ScrollToBottomAsync();

        try
        {
            if (_useStreaming)
            {
                await SendWithStreamingAsync(userMessage);
            }
            else
            {
                await SendWithoutStreamingAsync(userMessage);
            }
        }
        catch (OperationCanceledException)
        {
            // Usuario canceló
        }
        catch (Exception ex)
        {
            _messages.Add(new ChatMessageDto
            {
                Content = $"Error: {ex.Message}",
                IsUser = false,
                Timestamp = DateTime.UtcNow
            });
        }
        finally
        {
            _isProcessing = false;
            _streamingContent = "";
            _cts?.Dispose();
            _cts = null;
            await ScrollToBottomAsync();
        }
    }

    private async Task SendWithoutStreamingAsync(string userMessage)
    {
        var response = await ChatApi.SendMessageAsync(ProjectId, userMessage, _cts!.Token);

        if (response is null)
        {
            _messages.Add(new ChatMessageDto
            {
                Content = "Lo siento, hubo un error al procesar tu pregunta. Por favor, intenta de nuevo.",
                IsUser = false,
                Timestamp = DateTime.UtcNow
            });
            return;
        }

        _messages.Add(new ChatMessageDto
        {
            Content = response.Response,
            IsUser = false,
            Timestamp = DateTime.UtcNow,
            References = response.References
        });
    }

    private async Task SendWithStreamingAsync(string userMessage)
    {
        var contentBuilder = new System.Text.StringBuilder();
        var assistantMessage = new ChatMessageDto
        {
            Content = "",
            IsUser = false,
            Timestamp = DateTime.UtcNow,
            References = []
        };

        // Agregar mensaje vacío que iremos llenando
        _messages.Add(assistantMessage);

        await foreach (var evt in ChatApi.SendMessageStreamAsync(
            ProjectId, userMessage, _cts!.Token))
        {
            switch (evt.EventType)
            {
                case "token":
                    contentBuilder.Append(evt.Data);
                    assistantMessage.Content = contentBuilder.ToString();
                    _streamingContent = assistantMessage.Content;
                    await InvokeAsync(StateHasChanged);
                    break;

                case "error":
                    assistantMessage.Content = $"Error: {evt.Data}";
                    break;

                case "done":
                    // Streaming completado
                    break;
            }
        }

        // Actualizar contenido final
        if (contentBuilder.Length > 0)
        {
            assistantMessage.Content = contentBuilder.ToString();
        }
    }

    private void ClearChat()
    {
        _messages.Clear();
        _cts?.Cancel();
    }

    private async Task HandleReferenceClick(Guid documentId)
    {
        await OnDocumentReferenceClick.InvokeAsync(documentId);
    }

    private async Task ScrollToBottomAsync()
    {
        try
        {
            await JS.InvokeVoidAsync("scrollToBottom", _chatContainer);
        }
        catch
        {
            // Ignorar errores de JS interop durante prerendering
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
```

---

### 5. Modificar ChatMessage.razor para mostrar referencias

**Archivo:** `src/Babel.WebUI/Components/Chat/ChatMessage.razor`

Agregar sección de referencias al final del mensaje del asistente:

```razor
@if (!Message.IsUser && Message.References.Count > 0)
{
    <div class="mt-2 pt-2" style="border-top: 1px solid var(--mud-palette-divider);">
        <MudText Typo="Typo.caption" Color="Color.Secondary" Class="mb-1">
            <MudIcon Icon="@Icons.Material.Filled.Source" Size="Size.Small" Class="mr-1" />
            Fuentes (@Message.References.Count):
        </MudText>
        <div class="d-flex flex-wrap gap-1">
            @foreach (var reference in Message.References)
            {
                <MudChip T="string"
                         Size="Size.Small"
                         Color="Color.Primary"
                         Variant="Variant.Outlined"
                         OnClick="@(() => OnReferenceClick.InvokeAsync(reference.DocumentId))"
                         Style="cursor: pointer;">
                    <MudIcon Icon="@Icons.Material.Filled.Description" Size="Size.Small" Class="mr-1" />
                    @reference.FileName
                    <MudText Typo="Typo.caption" Class="ml-1">
                        (@(reference.RelevanceScore * 100:F0)%)
                    </MudText>
                </MudChip>
            }
        </div>
    </div>
}
```

---

### 6. Actualizar Detail.razor para resaltar documento referenciado

**Archivo:** `src/Babel.WebUI/Pages/Projects/Detail.razor`

Modificar `HandleDocumentClick` para resaltar el documento:

```csharp
private Guid? _highlightedDocumentId;

private void HandleDocumentClick(Guid documentId)
{
    _highlightedDocumentId = documentId;

    // Buscar el documento y hacer scroll
    var document = _documents.FirstOrDefault(d => d.Id == documentId);
    if (document != null)
    {
        Snackbar.Add($"Documento: {document.FileName}", Severity.Info);
    }

    StateHasChanged();

    // Quitar highlight después de 3 segundos
    _ = Task.Delay(3000).ContinueWith(_ =>
    {
        _highlightedDocumentId = null;
        InvokeAsync(StateHasChanged);
    });
}
```

Y en la tabla de documentos, agregar clase condicional:

```razor
<MudTr Style="@(_highlightedDocumentId == context.Id ? "background-color: var(--mud-palette-primary-lighten);" : "")">
```

---

## Checklist de Implementación

### Backend (Babel.API)
- [x] Crear `ChatController.cs`
- [x] Endpoint POST `/api/projects/{projectId}/chat`
- [x] Endpoint POST `/api/projects/{projectId}/chat/stream` (SSE)
- [x] Documentar endpoints en Swagger (via atributos ProducesResponseType)

### Frontend (Babel.WebUI)
- [x] Crear `Services/ChatApiService.cs`
- [x] Crear `Services/IChatApiService.cs` (interface)
- [x] Registrar `HttpClient` en `Program.cs`
- [x] Agregar `ApiBaseUrl` a `appsettings.json`
- [x] Modificar `ChatWindow.razor`:
  - [x] Inyectar `IChatApiService`
  - [x] Implementar `SendWithoutStreamingAsync`
  - [x] Implementar `SendWithStreamingAsync`
  - [x] Agregar toggle de streaming
  - [x] Manejar cancelación
- [x] Modificar `ChatMessage.razor`:
  - [x] Mostrar referencias como chips clicables
  - [x] Mostrar score de relevancia
- [x] Modificar `Detail.razor`:
  - [x] Resaltar documento al hacer click en referencia

### Tests
- [ ] Tests unitarios de `ChatController`
- [ ] Tests de `ChatApiService` (mock HttpClient)
- [ ] Test de integración del flujo completo

### Documentación
- [ ] Actualizar `PLAN_DESARROLLO.md`
- [x] Documentar endpoints en Swagger (via atributos)
- [ ] Agregar notas a `CLAUDE.md` si aplica

---

## Consideraciones Técnicas

### CORS
Si WebUI y API están en diferentes puertos/hosts, configurar CORS en `Babel.API`:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowWebUI", policy =>
    {
        policy.WithOrigins("https://localhost:5002") // Puerto de WebUI
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});
```

### Timeout
El streaming puede durar varios minutos. Configurar timeout adecuado:
- HttpClient: 5 minutos
- Kestrel: Sin límite para SSE

### Manejo de Errores
- Timeout de LLM → Mensaje amigable
- Proyecto sin documentos vectorizados → Mensaje informativo
- Error de red → Retry automático (opcional)

### Seguridad
- Validar que el usuario tiene acceso al proyecto (futuro)
- Sanitizar mensajes de usuario
- Rate limiting (futuro)

---

## Dependencias

**Paquetes necesarios:** Ninguno adicional (ya están instalados)

**Servicios requeridos:**
- SQL Server (documentos, chunks)
- Qdrant (búsqueda vectorial)
- LLM (OpenAI/Ollama configurado)

---

## Estimación

| Tarea | Complejidad |
|-------|-------------|
| ChatController | Baja |
| ChatApiService | Media |
| ChatWindow.razor (sin streaming) | Baja |
| ChatWindow.razor (con streaming) | Media |
| ChatMessage.razor (referencias) | Baja |
| Detail.razor (highlight) | Baja |
| Tests | Media |

---

## Resultado Esperado

Al completar esta tarea:

1. Usuario escribe pregunta en chat
2. WebUI llama a API via `ChatApiService`
3. API procesa con MediatR → `ChatQueryHandler` → `SemanticKernelChatService`
4. Respuesta (streaming o completa) se muestra en chat
5. Referencias a documentos aparecen como chips clicables
6. Click en referencia resalta documento en la lista

---

*Documento creado: 2026-02-01*
