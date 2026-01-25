namespace Babel.Application.Common;

/// <summary>
/// Representa un error de dominio con código y descripción.
/// </summary>
public sealed record Error(string Code, string Description)
{
    /// <summary>
    /// Representa la ausencia de error.
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>
    /// Error genérico para valores nulos.
    /// </summary>
    public static readonly Error NullValue = new(
        "Error.NullValue",
        "El valor proporcionado es nulo.");

    /// <summary>
    /// Crea un error de validación.
    /// </summary>
    public static Error Validation(string code, string description) =>
        new($"Validation.{code}", description);
}
