using Babel.API.Contracts;
using Babel.Application.DTOs;
using Babel.Application.Projects.Commands;
using Babel.Application.Projects.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Babel.API.Controllers;

/// <summary>
/// Controlador REST para la gestión de proyectos.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProjectsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(IMediator mediator, ILogger<ProjectsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene todos los proyectos.
    /// </summary>
    /// <returns>Lista de proyectos con conteos de documentos</returns>
    [HttpGet]
    [ProducesResponseType(typeof(List<ProjectDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all projects");

        var result = await _mediator.Send(new GetProjectsQuery(), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error.Description, statusCode: 500);
    }

    /// <summary>
    /// Obtiene un proyecto por su ID.
    /// </summary>
    /// <param name="id">ID del proyecto</param>
    /// <returns>Proyecto con detalle</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting project {ProjectId}", id);

        var result = await _mediator.Send(new GetProjectByIdQuery(id), cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error.Code == "Project.NotFound")
                return NotFound(new { error = result.Error.Description });

            return Problem(result.Error.Description, statusCode: 500);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Busca proyectos por nombre.
    /// </summary>
    /// <param name="term">Término de búsqueda</param>
    /// <returns>Lista de proyectos que coinciden con la búsqueda</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<ProjectDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string term,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Searching projects with term: {SearchTerm}", term);

        var result = await _mediator.Send(new SearchProjectsQuery(term), cancellationToken);

        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error.Description, statusCode: 500);
    }

    /// <summary>
    /// Crea un nuevo proyecto.
    /// </summary>
    /// <param name="request">Datos del proyecto</param>
    /// <returns>Proyecto creado</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating project: {ProjectName}", request.Name);

        var command = new CreateProjectCommand(request.Name, request.Description);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error.Code == "Project.DuplicateName")
                return Conflict(new { error = result.Error.Description });

            if (result.Error.Code == "Validation.Error")
                return BadRequest(new { error = result.Error.Description });

            return Problem(result.Error.Description, statusCode: 500);
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value!.Id },
            result.Value);
    }

    /// <summary>
    /// Actualiza un proyecto existente.
    /// </summary>
    /// <param name="id">ID del proyecto</param>
    /// <param name="request">Datos actualizados</param>
    /// <returns>Proyecto actualizado</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProjectDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating project {ProjectId}", id);

        var command = new UpdateProjectCommand(id, request.Name, request.Description);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error.Code == "Project.NotFound")
                return NotFound(new { error = result.Error.Description });

            if (result.Error.Code == "Project.DuplicateName")
                return Conflict(new { error = result.Error.Description });

            if (result.Error.Code == "Validation.Error")
                return BadRequest(new { error = result.Error.Description });

            return Problem(result.Error.Description, statusCode: 500);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Elimina un proyecto y todos sus documentos.
    /// </summary>
    /// <param name="id">ID del proyecto</param>
    /// <returns>NoContent si se eliminó correctamente</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting project {ProjectId}", id);

        var result = await _mediator.Send(new DeleteProjectCommand(id), cancellationToken);

        if (!result.IsSuccess)
        {
            if (result.Error.Code == "Project.NotFound")
                return NotFound(new { error = result.Error.Description });

            return Problem(result.Error.Description, statusCode: 500);
        }

        return NoContent();
    }
}
