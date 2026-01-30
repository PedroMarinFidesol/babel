using Babel.Application.Common;
using Babel.Application.DTOs;
using Babel.Application.Interfaces;

namespace Babel.Application.Chat.Queries;

/// <summary>
/// Handler para ChatQuery. Delega al servicio de chat RAG.
/// </summary>
public sealed class ChatQueryHandler : IQueryHandler<ChatQuery, ChatResponseDto>
{
    private readonly IChatService _chatService;

    public ChatQueryHandler(IChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task<Result<ChatResponseDto>> Handle(
        ChatQuery request,
        CancellationToken cancellationToken)
    {
        // Validación básica
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return Result.Failure<ChatResponseDto>(DomainErrors.Chat.MessageRequired);
        }

        if (request.Message.Length > 10000)
        {
            return Result.Failure<ChatResponseDto>(DomainErrors.Chat.MessageTooLong);
        }

        // Delegar al servicio de chat
        return await _chatService.ChatAsync(
            request.ProjectId,
            request.Message,
            cancellationToken);
    }
}
