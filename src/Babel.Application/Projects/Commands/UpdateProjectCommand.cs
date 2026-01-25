using Babel.Application.Common;
using Babel.Application.DTOs;

namespace Babel.Application.Projects.Commands;

/// <summary>
/// Command para actualizar un proyecto existente.
/// </summary>
public sealed record UpdateProjectCommand(
    Guid Id,
    string Name,
    string? Description) : ICommand<ProjectDto>;
