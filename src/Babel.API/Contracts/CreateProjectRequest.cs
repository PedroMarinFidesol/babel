namespace Babel.API.Contracts;

/// <summary>
/// Request para crear un nuevo proyecto.
/// </summary>
public sealed record CreateProjectRequest(
    string Name,
    string? Description);
