using Babel.Application.Common;

namespace Babel.Application.Documents.Commands;

/// <summary>
/// Command para eliminar un documento.
/// Elimina el archivo del storage y el registro de la base de datos.
/// </summary>
/// <param name="Id">ID del documento a eliminar</param>
public sealed record DeleteDocumentCommand(Guid Id) : ICommand;
