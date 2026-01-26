using Babel.Application.Common;
using Babel.Application.DTOs;

namespace Babel.Application.Documents.Queries;

/// <summary>
/// Query para obtener un documento por su ID.
/// </summary>
/// <param name="Id">ID del documento</param>
public sealed record GetDocumentByIdQuery(Guid Id) : IQuery<DocumentDto>;
