using Babel.Application.Common;
using Babel.Application.DTOs;

namespace Babel.Application.Projects.Queries;

/// <summary>
/// Query para buscar proyectos por nombre.
/// </summary>
/// <param name="SearchTerm">Término de búsqueda (parcial)</param>
public sealed record SearchProjectsQuery(string SearchTerm) : IQuery<List<ProjectDto>>;
