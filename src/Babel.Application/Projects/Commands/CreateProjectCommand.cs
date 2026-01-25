using Babel.Application.Common;
using Babel.Application.DTOs;

namespace Babel.Application.Projects.Commands;

/// <summary>
/// Command para crear un nuevo proyecto.
/// </summary>
public sealed record CreateProjectCommand(
    string Name,
    string? Description) : ICommand<ProjectDto>;
