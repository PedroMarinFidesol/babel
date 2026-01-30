using Babel.Application.Common;
using Babel.Application.DTOs;

namespace Babel.Application.Chat.Queries;

/// <summary>
/// Query para ejecutar chat RAG sobre los documentos de un proyecto.
/// </summary>
/// <param name="ProjectId">ID del proyecto donde buscar contexto</param>
/// <param name="Message">Mensaje/pregunta del usuario</param>
public sealed record ChatQuery(Guid ProjectId, string Message) : IQuery<ChatResponseDto>;
