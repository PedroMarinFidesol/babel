using FluentValidation;

namespace Babel.Application.Chat.Queries;

/// <summary>
/// Validador para ChatQuery.
/// </summary>
public sealed class ChatQueryValidator : AbstractValidator<ChatQuery>
{
    public ChatQueryValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithMessage("El ID del proyecto es requerido.");

        RuleFor(x => x.Message)
            .NotEmpty()
            .WithMessage("El mensaje de consulta es requerido.")
            .MaximumLength(10000)
            .WithMessage("El mensaje no puede exceder 10000 caracteres.");
    }
}
