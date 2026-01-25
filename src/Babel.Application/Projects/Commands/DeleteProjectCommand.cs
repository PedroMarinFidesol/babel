using Babel.Application.Common;

namespace Babel.Application.Projects.Commands;

/// <summary>
/// Command para eliminar un proyecto.
/// </summary>
public sealed record DeleteProjectCommand(Guid Id) : ICommand;
