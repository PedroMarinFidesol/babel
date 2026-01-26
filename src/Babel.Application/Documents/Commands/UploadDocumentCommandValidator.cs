using FluentValidation;

namespace Babel.Application.Documents.Commands;

/// <summary>
/// Validador para UploadDocumentCommand.
/// </summary>
public sealed class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
{
    public UploadDocumentCommandValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithMessage("El ID del proyecto es requerido.");

        RuleFor(x => x.FileName)
            .NotEmpty()
            .WithMessage("El nombre del archivo es requerido.")
            .MaximumLength(255)
            .WithMessage("El nombre del archivo no puede exceder 255 caracteres.");

        RuleFor(x => x.FileStream)
            .NotNull()
            .WithMessage("El contenido del archivo es requerido.");
    }
}
