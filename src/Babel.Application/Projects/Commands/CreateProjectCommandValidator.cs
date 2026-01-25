using FluentValidation;

namespace Babel.Application.Projects.Commands;

/// <summary>
/// Validador para CreateProjectCommand.
/// </summary>
public sealed class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("El nombre del proyecto es requerido.")
            .MaximumLength(200)
            .WithMessage("El nombre del proyecto no puede exceder 200 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("La descripción no puede exceder 1000 caracteres.")
            .When(x => x.Description != null);
    }
}
