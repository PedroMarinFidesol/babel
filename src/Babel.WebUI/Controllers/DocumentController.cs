using Babel.Application.Documents.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Babel.WebUI.Controllers;

[ApiController]
[Produces("application/json")]
public class DocumentController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<DocumentController> _logger;

    public DocumentController(IMediator mediator, ILogger<DocumentController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet("api/documents/{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
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
}
