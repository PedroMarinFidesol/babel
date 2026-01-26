using Babel.Application.Common;
using Babel.Application.DTOs;

namespace Babel.Application.Documents.Commands;

/// <summary>
/// Command para subir un documento a un proyecto.
/// </summary>
/// <param name="ProjectId">ID del proyecto destino</param>
/// <param name="FileName">Nombre original del archivo</param>
/// <param name="FileStream">Stream con el contenido del archivo</param>
public sealed record UploadDocumentCommand(
    Guid ProjectId,
    string FileName,
    Stream FileStream) : ICommand<DocumentDto>;
