using Babel.Application.Documents.Commands;
using Babel.Application.Documents.Queries;
using Babel.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Babel.API.Controllers;

/// <summary>
/// Controlador REST para la gestión de documentos.
/// </summary>
[ApiController]
[Produces("application/json")]
public class DocumentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(IMediator mediator, ILogger<DocumentsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Sube un documento a un proyecto.
    /// </summary>
    /// <param name="projectId">ID del proyecto</param>
    /// <param name="file">Archivo a subir</param>
    /// <returns>Documento creado</returns>
    [HttpPost("api/projects/{projectId:guid}/documents")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Upload(
        Guid projectId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No se proporcionó ningún archivo." });
        }

        _logger.LogInformation(
            "Uploading document {FileName} to project {ProjectId}",
            file.FileName, projectId);

        await using var stream = file.OpenReadStream();
        var command = new UploadDocumentCommand(projectId, file.FileName, stream);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.Error.Code switch
            {
                "Document.ProjectNotFound" => NotFound(new { error = result.Error.Description }),
                "Document.UnsupportedFileType" => BadRequest(new { error = result.Error.Description }),
                "Document.FileTooLarge" => BadRequest(new { error = result.Error.Description }),
                "Document.DuplicateFile" => Conflict(new { error = result.Error.Description }),
                _ => Problem(result.Error.Description, statusCode: 500)
            };
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value!.Id },
            result.Value);
    }

    /// <summary>
    /// Obtiene todos los documentos de un proyecto.
    /// </summary>
    /// <param name="projectId">ID del proyecto</param>
    /// <returns>Lista de documentos</returns>
    [HttpGet("api/projects/{projectId:guid}/documents")]
    [ProducesResponseType(typeof(List<DocumentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByProject(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting documents for project {ProjectId}", projectId);

        var result = await _mediator.Send(new GetDocumentsByProjectQuery(projectId), cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error.Code == "Document.ProjectNotFound")
                return NotFound(new { error = result.Error.Description });

            return Problem(result.Error.Description, statusCode: 500);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Obtiene un documento por su ID.
    /// </summary>
    /// <param name="id">ID del documento</param>
    /// <returns>Documento</returns>
    [HttpGet("api/documents/{id:guid}")]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting document {DocumentId}", id);

        var result = await _mediator.Send(new GetDocumentByIdQuery(id), cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error.Code == "Document.NotFound")
                return NotFound(new { error = result.Error.Description });

            return Problem(result.Error.Description, statusCode: 500);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Descarga el contenido de un documento.
    /// </summary>
    /// <param name="id">ID del documento</param>
    /// <returns>Archivo para descarga</returns>
    [HttpGet("api/documents/{id:guid}/download")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Downloading document {DocumentId}", id);

        var result = await _mediator.Send(new GetDocumentContentQuery(id), cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error.Code == "Document.NotFound")
                return NotFound(new { error = result.Error.Description });

            return Problem(result.Error.Description, statusCode: 500);
        }

        var content = result.Value!;
        return File(content.Content, content.MimeType, content.FileName);
    }

    /// <summary>
    /// Elimina un documento.
    /// </summary>
    /// <param name="id">ID del documento</param>
    /// <returns>NoContent si se eliminó correctamente</returns>
    [HttpDelete("api/documents/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting document {DocumentId}", id);

        var result = await _mediator.Send(new DeleteDocumentCommand(id), cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error.Code == "Document.NotFound")
                return NotFound(new { error = result.Error.Description });

            return Problem(result.Error.Description, statusCode: 500);
        }

        return NoContent();
    }
}
