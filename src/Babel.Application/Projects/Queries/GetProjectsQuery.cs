using Babel.Application.Common;
using Babel.Application.DTOs;

namespace Babel.Application.Projects.Queries;

/// <summary>
/// Query para obtener todos los proyectos con sus conteos de documentos.
/// </summary>
public sealed record GetProjectsQuery : IQuery<List<ProjectDto>>;
