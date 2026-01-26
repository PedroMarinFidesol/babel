namespace Babel.API.Contracts;

/// <summary>
/// Request para actualizar un proyecto existente.
/// </summary>
public sealed record UpdateProjectRequest(
    string Name,
    string? Description);
