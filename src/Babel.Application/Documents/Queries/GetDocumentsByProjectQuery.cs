using Babel.Application.Common;
using Babel.Application.DTOs;

namespace Babel.Application.Documents.Queries;

/// <summary>
/// Query para obtener todos los documentos de un proyecto.
/// </summary>
/// <param name="ProjectId">ID del proyecto</param>
public sealed record GetDocumentsByProjectQuery(Guid ProjectId) : IQuery<List<DocumentDto>>;
