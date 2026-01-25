using Babel.Application.Common;
using Babel.Application.DTOs;

namespace Babel.Application.Projects.Queries;

/// <summary>
/// Query para obtener un proyecto por su ID.
/// </summary>
public sealed record GetProjectByIdQuery(Guid Id) : IQuery<ProjectDto>;
