using System.Runtime.CompilerServices;
using System.Text;
using Babel.Application.Common;
using Babel.Application.DTOs;
using Babel.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Babel.Infrastructure.Services;

/// <summary>
/// Servicio de chat con patrón RAG usando Semantic Kernel.
/// Implementa recuperación de contexto desde Qdrant y generación con LLM.
/// </summary>
public class SemanticKernelChatService : IChatService
{
    private readonly Kernel _kernel;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStoreService _vectorStoreService;
    private readonly IApplicationDbContext _dbContext;
    private readonly IProjectRepository _projectRepository;
    private readonly ILogger<SemanticKernelChatService> _logger;

    private const int DefaultTopK = 5;
    private const float DefaultMinScore = 0.3f;
    private const int MaxContextTokens = 4000;

    private const string SystemPrompt = """
        Eres un asistente experto que responde preguntas basándose EXCLUSIVAMENTE en los documentos proporcionados.

        REGLAS IMPORTANTES:
        1. Usa SOLO la información del contexto para responder. No inventes información.
        2. Si la información no está en el contexto, di claramente: "No encuentro información sobre esto en los documentos del proyecto."
        3. Cita los documentos fuente cuando sea relevante (menciona el nombre del archivo).
        4. Responde en el mismo idioma que la pregunta.
        5. Sé conciso pero completo.
        """;

    public SemanticKernelChatService(
        Kernel kernel,
        IEmbeddingService embeddingService,
        IVectorStoreService vectorStoreService,
        IApplicationDbContext dbContext,
        IProjectRepository projectRepository,
        ILogger<SemanticKernelChatService> logger)
    {
        _kernel = kernel;
        _embeddingService = embeddingService;
        _vectorStoreService = vectorStoreService;
        _dbContext = dbContext;
        _projectRepository = projectRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<ChatResponseDto>> ChatAsync(
        Guid projectId,
        string message,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Iniciando chat RAG. ProjectId: {ProjectId}, Message: {Message}",
            projectId, message.Length > 100 ? message[..100] + "..." : message);

        // 1. Validar que el proyecto existe
        var projectResult = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (projectResult is null)
        {
            return Result.Failure<ChatResponseDto>(
                new Error("Chat.ProjectNotFound", $"Proyecto con ID {projectId} no encontrado."));
        }

        // 2. Verificar que hay documentos vectorizados
        var hasVectorizedDocs = await _dbContext.Documents
            .AnyAsync(d => d.ProjectId == projectId && d.IsVectorized, cancellationToken);

        if (!hasVectorizedDocs)
        {
            return Result.Success(new ChatResponseDto
            {
                Response = "Este proyecto no tiene documentos procesados todavía. " +
                          "Por favor, sube algunos documentos y espera a que se procesen.",
                References = []
            });
        }

        // 3. Generar embedding de la consulta
        var embeddingResult = await _embeddingService.GenerateEmbeddingAsync(message, cancellationToken);
        if (embeddingResult.IsFailure)
        {
            _logger.LogError("Error generando embedding para consulta: {Error}", embeddingResult.Error);
            return Result.Failure<ChatResponseDto>(embeddingResult.Error);
        }

        // 4. Buscar chunks similares en Qdrant
        var searchResult = await _vectorStoreService.SearchAsync(
            embeddingResult.Value,
            projectId,
            DefaultTopK,
            DefaultMinScore,
            cancellationToken);

        if (searchResult.IsFailure)
        {
            _logger.LogError("Error en búsqueda vectorial: {Error}", searchResult.Error);
            return Result.Failure<ChatResponseDto>(searchResult.Error);
        }

        var vectorResults = searchResult.Value;
        _logger.LogDebug("Búsqueda vectorial completada. Resultados: {Count}", vectorResults.Count);

        // 5. Recuperar contenido de chunks desde la BD
        var (context, references) = await BuildContextAsync(vectorResults, cancellationToken);

        // Eliminar duplicados de referencias (mismo DocumentId)
        var uniqueReferences = references
            .GroupBy(r => r.DocumentId)
            .Select(g => g.OrderByDescending(r => r.RelevanceScore).First())
            .ToList();

        if (string.IsNullOrWhiteSpace(context))
        {
            return Result.Success(new ChatResponseDto
            {
                Response = "No encontré documentos relevantes para tu pregunta en este proyecto.",
                References = uniqueReferences
            });
        }

        // 6. Construir prompt y llamar al LLM
        var response = await CallLlmAsync(context, message, cancellationToken);

        return Result.Success(new ChatResponseDto
        {
            Response = response,
            References = uniqueReferences
        });
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> ChatStreamAsync(
        Guid projectId,
        string message,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Iniciando chat RAG streaming. ProjectId: {ProjectId}", projectId);

        var projectResult = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (projectResult is null)
        {
            yield return "Error: Proyecto no encontrado.";
            yield break;
        }

        var hasVectorizedDocs = await _dbContext.Documents
            .AnyAsync(d => d.ProjectId == projectId && d.IsVectorized, cancellationToken);

        if (!hasVectorizedDocs)
        {
            yield return "Este proyecto no tiene documentos procesados todavía. " +
                        "Por favor, sube algunos documentos y espera a que se procesen.";
            yield break;
        }

        var embeddingResult = await _embeddingService.GenerateEmbeddingAsync(message, cancellationToken);
        if (embeddingResult.IsFailure)
        {
            yield return $"Error generando embedding: {embeddingResult.Error.Description}";
            yield break;
        }

        var searchResult = await _vectorStoreService.SearchAsync(
            embeddingResult.Value,
            projectId,
            DefaultTopK,
            DefaultMinScore,
            cancellationToken);

        if (searchResult.IsFailure)
        {
            yield return $"Error en búsqueda: {searchResult.Error.Description}";
            yield break;
        }

        var (context, _) = await BuildContextAsync(searchResult.Value, cancellationToken);

        if (string.IsNullOrWhiteSpace(context))
        {
            yield return "No encontré documentos relevantes para tu pregunta en este proyecto.";
            yield break;
        }

        await foreach (var token in StreamLlmResponseAsync(context, message, cancellationToken))
        {
            yield return token;
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<(object, List<DocumentReferenceDto>)> ChatStreamWithReferencesAsync(
        Guid projectId,
        string message,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Iniciando chat RAG streaming con referencias. ProjectId: {ProjectId}", projectId);

        var projectResult = await _projectRepository.GetByIdAsync(projectId, cancellationToken);
        if (projectResult is null)
        {
            yield return ("Error: Proyecto no encontrado.", []);
            yield break;
        }

        var hasVectorizedDocs = await _dbContext.Documents
            .AnyAsync(d => d.ProjectId == projectId && d.IsVectorized, cancellationToken);

        if (!hasVectorizedDocs)
        {
            yield return ("Este proyecto no tiene documentos procesados todavía. " +
                        "Por favor, sube algunos documentos y espera a que se procesen.", []);
            yield break;
        }

        var embeddingResult = await _embeddingService.GenerateEmbeddingAsync(message, cancellationToken);
        if (embeddingResult.IsFailure)
        {
            yield return ($"Error generando embedding: {embeddingResult.Error.Description}", []);
            yield break;
        }

        var searchResult = await _vectorStoreService.SearchAsync(
            embeddingResult.Value,
            projectId,
            DefaultTopK,
            DefaultMinScore,
            cancellationToken);

        if (searchResult.IsFailure)
        {
            yield return ($"Error en búsqueda: {searchResult.Error.Description}", []);
            yield break;
        }

        var (context, references) = await BuildContextAsync(searchResult.Value, cancellationToken);

        if (string.IsNullOrWhiteSpace(context))
        {
            yield return ("No encontré documentos relevantes para tu pregunta en este proyecto.", []);
            yield break;
        }

        // Eliminar duplicados de referencias (mismo DocumentId)
        var uniqueReferences = references
            .GroupBy(r => r.DocumentId)
            .Select(g => g.OrderByDescending(r => r.RelevanceScore).First())
            .ToList();

        await foreach (var item in StreamLlmWithReferencesAsync(context, message, uniqueReferences, cancellationToken))
        {
            yield return item;
        }
    }

    /// <summary>
    /// Construye el contexto RAG a partir de los resultados de búsqueda vectorial.
    /// Recupera el contenido de los chunks desde la base de datos.
    /// </summary>
    private async Task<(string Context, List<DocumentReferenceDto> References)> BuildContextAsync(
        IReadOnlyList<VectorSearchResult> vectorResults,
        CancellationToken cancellationToken)
    {
        if (vectorResults.Count == 0)
        {
            return (string.Empty, []);
        }

        // Obtener los IDs de documentos y chunks para consultar la BD
        var chunkKeys = vectorResults
            .Select(r => new { r.DocumentId, r.ChunkIndex })
            .ToList();

        var documentIds = vectorResults.Select(r => r.DocumentId).Distinct().ToList();

        // Cargar chunks desde la BD
        var chunks = await _dbContext.DocumentChunks
            .Include(c => c.Document)
            .Where(c => documentIds.Contains(c.DocumentId))
            .ToListAsync(cancellationToken);

        // Filtrar por los chunks específicos encontrados
        var relevantChunks = chunks
            .Where(c => chunkKeys.Any(k => k.DocumentId == c.DocumentId && k.ChunkIndex == c.ChunkIndex))
            .ToList();

        // Construir contexto concatenando chunks
        var contextBuilder = new StringBuilder();
        var references = new List<DocumentReferenceDto>();
        var seenDocuments = new HashSet<Guid>();
        var estimatedTokens = 0;

        foreach (var vectorResult in vectorResults)
        {
            var chunk = relevantChunks.FirstOrDefault(c =>
                c.DocumentId == vectorResult.DocumentId &&
                c.ChunkIndex == vectorResult.ChunkIndex);

            if (chunk is null) continue;

            // Estimar tokens (aproximación: 4 caracteres = 1 token)
            var chunkTokens = chunk.Content.Length / 4;
            if (estimatedTokens + chunkTokens > MaxContextTokens) break;

            contextBuilder.AppendLine($"[Documento: {chunk.Document.FileName}]");
            contextBuilder.AppendLine(chunk.Content);
            contextBuilder.AppendLine();

            estimatedTokens += chunkTokens;

            // Agregar referencia si no se ha agregado antes
            if (!seenDocuments.Contains(vectorResult.DocumentId))
            {
                seenDocuments.Add(vectorResult.DocumentId);
                references.Add(new DocumentReferenceDto
                {
                    DocumentId = vectorResult.DocumentId,
                    FileName = vectorResult.FileName,
                    Snippet = chunk.Content.Length > 200
                        ? chunk.Content[..200] + "..."
                        : chunk.Content,
                    RelevanceScore = vectorResult.SimilarityScore,
                    ChunkIndex = vectorResult.ChunkIndex
                });
            }
        }

        return (contextBuilder.ToString(), references);
    }

    /// <summary>
    /// Llama al LLM con el contexto y la pregunta del usuario.
    /// </summary>
    private async Task<string> CallLlmAsync(
        string context,
        string userMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            var chatService = _kernel.GetRequiredService<IChatCompletionService>();

            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(SystemPrompt);
            chatHistory.AddUserMessage($"""
                CONTEXTO DE DOCUMENTOS:
                {context}

                PREGUNTA DEL USUARIO:
                {userMessage}
                """);

            var response = await chatService.GetChatMessageContentAsync(
                chatHistory,
                cancellationToken: cancellationToken);

            return response?.Content ?? "No se pudo generar una respuesta.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error llamando al LLM");
            return $"Error generando respuesta: {ex.Message}";
        }
    }

    /// <summary>
    /// Stream de respuesta del LLM.
    /// </summary>
    private async IAsyncEnumerable<string> StreamLlmResponseAsync(
        string context,
        string userMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var (chatService, errorMessage) = GetChatService();

        if (chatService is null)
        {
            yield return errorMessage ?? "Error: No hay servicio de chat configurado.";
            yield break;
        }

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(SystemPrompt);
        chatHistory.AddUserMessage($"""
            CONTEXTO DE DOCUMENTOS:
            {context}

            PREGUNTA DEL USUARIO:
            {userMessage}
            """);

        await foreach (var chunk in chatService.GetStreamingChatMessageContentsAsync(
            chatHistory,
            cancellationToken: cancellationToken))
        {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
                yield return chunk.Content;
            }
        }
    }

    /// <summary>
    /// Stream de respuesta del LLM con referencias.
    /// </summary>
    private async IAsyncEnumerable<(object, List<DocumentReferenceDto>)> StreamLlmWithReferencesAsync(
        string context,
        string userMessage,
        List<DocumentReferenceDto> references,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var (chatService, errorMessage) = GetChatService();

        if (chatService is null)
        {
            yield return (errorMessage ?? "Error: No hay servicio de chat configurado.", []);
            yield break;
        }

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(SystemPrompt);
        chatHistory.AddUserMessage($"""
            CONTEXTO DE DOCUMENTOS:
            {context}

            PREGUNTA DEL USUARIO:
            {userMessage}
            """);

        await foreach (var chunk in chatService.GetStreamingChatMessageContentsAsync(
            chatHistory,
            cancellationToken: cancellationToken))
        {
            if (!string.IsNullOrEmpty(chunk.Content))
            {
                yield return (chunk.Content, []);
            }
        }

        yield return (new { __references = true }, references);
    }

    /// <summary>
    /// Obtiene el servicio de chat del kernel.
    /// Separado para evitar yield dentro de try-catch.
    /// </summary>
    private (IChatCompletionService? Service, string? Error) GetChatService()
    {
        try
        {
            return (_kernel.GetRequiredService<IChatCompletionService>(), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo servicio de chat");
            return (null, $"Error: No hay servicio de chat configurado. {ex.Message}");
        }
    }
}

