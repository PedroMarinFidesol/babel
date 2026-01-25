using FluentValidation;

namespace Babel.Application.Projects.Commands;

/// <summary>
/// Validador para UpdateProjectCommand.
/// </summary>
public sealed class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("El ID del proyecto es requerido.");

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
