# Sesion 2026-01-30 17:02 - Servicio de Chat RAG (Fase 9.2 y 9.3)

## Resumen

Implementacion completa del servicio de Chat RAG (Retrieval Augmented Generation) que permite hacer preguntas sobre documentos de un proyecto y obtener respuestas con referencias a los documentos fuente.

## Cambios Implementados

### 1. Busqueda Vectorial en IVectorStoreService

**Archivo:** `src/Babel.Application/Interfaces/IVectorStoreService.cs`

Se agrego:
- Record `VectorSearchResult` para resultados de busqueda vectorial
- Metodo `SearchAsync()` para busqueda por similitud con filtro por proyecto

```csharp
public record VectorSearchResult(
    Guid PointId,
    Guid DocumentId,
    Guid ProjectId,
    int ChunkIndex,
    string FileName,
    float SimilarityScore);

Task<Result<IReadOnlyList<VectorSearchResult>>> SearchAsync(
    ReadOnlyMemory<float> queryVector,
    Guid projectId,
    int topK = 5,
    float minScore = 0.7f,
    CancellationToken cancellationToken = default);
```

### 2. Implementacion de SearchAsync en Qdrant

**Archivo:** `src/Babel.Infrastructure/Services/QdrantVectorStoreService.cs`

Implementacion de busqueda vectorial usando `_qdrantClient.SearchAsync()` con:
- Filtro por `project_id` para limitar resultados al proyecto
- Threshold de score minimo configurable
- Limite de resultados (topK)
- Extraccion de payload con metadata del chunk

### 3. Configuracion de Chat Completion en Semantic Kernel

**Archivo:** `src/Babel.Infrastructure/DependencyInjection.cs`

Se agrego soporte para Chat Completion segun el proveedor:
- `AddOllamaChatCompletion()` para modelos locales
- `AddOpenAIChatCompletion()` para OpenAI API

### 4. Interfaz IChatService

**Archivo nuevo:** `src/Babel.Application/Interfaces/IChatService.cs`

```csharp
public interface IChatService
{
    Task<Result<ChatResponseDto>> ChatAsync(
        Guid projectId,
        string message,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> ChatStreamAsync(
        Guid projectId,
        string message,
        CancellationToken cancellationToken = default);
}
```

### 5. Implementacion del Servicio RAG

**Archivo nuevo:** `src/Babel.Infrastructure/Services/SemanticKernelChatService.cs`

Flujo RAG completo:

1. **Validacion** - Verifica proyecto existe y tiene documentos vectorizados
2. **Embedding** - Genera vector de la consulta con `IEmbeddingService`
3. **Busqueda** - Busca chunks similares en Qdrant filtrados por proyecto
4. **Contexto** - Recupera contenido de chunks desde SQL Server
5. **LLM** - Llama al modelo con prompt RAG estructurado
6. **Respuesta** - Retorna `ChatResponseDto` con respuesta y referencias

Prompt RAG:
```
Eres un asistente experto que responde preguntas basandose EXCLUSIVAMENTE en los documentos proporcionados.

REGLAS IMPORTANTES:
1. Usa SOLO la informacion del contexto para responder. No inventes informacion.
2. Si la informacion no esta en el contexto, di claramente: "No encuentro informacion sobre esto en los documentos del proyecto."
3. Cita los documentos fuente cuando sea relevante (menciona el nombre del archivo).
4. Responde en el mismo idioma que la pregunta.
5. Sé conciso pero completo.
```

### 6. Query de MediatR para Chat

**Archivos nuevos:**
- `src/Babel.Application/Chat/Queries/ChatQuery.cs`
- `src/Babel.Application/Chat/Queries/ChatQueryHandler.cs`
- `src/Babel.Application/Chat/Queries/ChatQueryValidator.cs`

Sigue el patron CQRS existente con:
- Query record con ProjectId y Message
- Handler que delega a IChatService
- Validator con FluentValidation

### 7. Errores de Dominio para Chat

**Archivo:** `src/Babel.Application/Common/DomainErrors.cs`

Se agrego clase estatica `DomainErrors.Chat` con:
- `ProjectNotFound`
- `NoVectorizedDocuments`
- `MessageRequired`
- `MessageTooLong`
- `EmbeddingFailed`
- `SearchFailed`
- `LlmFailed`

## Problemas Encontrados y Soluciones

### Error CS1631: yield return en catch

**Problema:** El metodo `StreamLlmResponseAsync` usaba `yield return` dentro de un bloque catch, lo cual no esta permitido en C#.

**Solucion:** Se extrajo la logica de obtencion del servicio de chat a un metodo separado `GetChatService()` que retorna una tupla `(IChatCompletionService?, string?)` permitiendo manejar el error fuera del contexto de yield.

```csharp
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
```

## Archivos Creados

| Archivo | Descripcion |
|---------|-------------|
| `src/Babel.Application/Interfaces/IChatService.cs` | Interfaz del servicio RAG |
| `src/Babel.Infrastructure/Services/SemanticKernelChatService.cs` | Implementacion RAG |
| `src/Babel.Application/Chat/Queries/ChatQuery.cs` | Query de MediatR |
| `src/Babel.Application/Chat/Queries/ChatQueryHandler.cs` | Handler de MediatR |
| `src/Babel.Application/Chat/Queries/ChatQueryValidator.cs` | Validador |

## Archivos Modificados

| Archivo | Cambios |
|---------|---------|
| `src/Babel.Application/Interfaces/IVectorStoreService.cs` | + VectorSearchResult, + SearchAsync |
| `src/Babel.Infrastructure/Services/QdrantVectorStoreService.cs` | + implementacion SearchAsync |
| `src/Babel.Infrastructure/DependencyInjection.cs` | + Chat Completion, + IChatService |
| `src/Babel.Application/Common/DomainErrors.cs` | + DomainErrors.Chat |

## Estado de Tests

- **111 tests pasan** (27 domain + 84 application)
- Compilacion exitosa sin errores

## Proximos Pasos

1. **Conectar ChatWindow.razor con ChatQuery** - Usar MediatR para enviar mensajes
2. **Endpoint REST para Chat** - Crear ChatController con endpoints
3. **Tests unitarios para ChatQueryHandler** - Mockear IChatService
4. **Tests de integracion** - Probar flujo completo RAG

## Comandos Utiles

```bash
# Compilar proyecto
dotnet build src/Babel.Infrastructure/Babel.Infrastructure.csproj

# Ejecutar tests
dotnet test

# Verificar Qdrant
curl http://localhost:6333/collections
```

## Lecciones Aprendidas

1. **yield y excepciones**: No se puede usar `yield return` dentro de bloques try-catch. La solucion es extraer la logica que puede fallar a un metodo separado.

2. **Streaming en RAG**: Para implementar streaming de respuestas, se debe usar `IAsyncEnumerable<string>` con `GetStreamingChatMessageContentsAsync()` de Semantic Kernel.

3. **Filtrado en Qdrant**: El filtrado por proyecto se hace usando un filtro `Must` con `FieldCondition` sobre el campo `project_id` del payload.

## Estado Final del Proyecto

- [x] Busqueda vectorial con filtro por proyecto
- [x] Chat Completion configurado (OpenAI/Ollama)
- [x] Servicio RAG completo con contexto y referencias
- [x] Query de MediatR con validacion
- [x] Errores de dominio definidos
- [ ] Conexion UI con backend (pendiente)
- [ ] Endpoint REST (pendiente)
- [ ] Tests del servicio de chat (pendiente)
