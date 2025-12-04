using Asp.Versioning;
using IdempotentAPI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskSystem.Dtos;
using TaskSystem.Dtos.Project;
using TaskSystem.Services.Projects.Interfaces;
using TaskSystem.Services.Shaper.Interfaces;
using TaskSystem.Types;
using TaskSystem.Validators;

namespace TaskSystem.Controllers.v2;

[Authorize(AuthenticationSchemes = "Access", Roles = $"{RolesType.User}")]
[ApiController]
[Route("api/v{version:apiVersion}/project")]
[Produces("application/json")]
[ApiVersion(2.0)]
public class ProjectController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly IDataShaper<ProjectGetDto> _dataShaper;

    public ProjectController(IProjectService projectService, IDataShaper<ProjectGetDto> dataShaper)
    {
        _projectService = projectService;
        _dataShaper = dataShaper;
    }

    [HttpGet("GetAll")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetAllProjects([FromQuery] string? include, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
            return Unauthorized();

        var validator = new PaginationValidator(pageNumber, pageSize);

        var result = await _projectService.GetAllProjects(validator.PageNumber, validator.PageSize);

        if (result.IsFailure)
            return NotFound(result.Error);

        return string.IsNullOrWhiteSpace(include)
            ? Ok(new
            {
                message = "Список проектов.",
                projects = result.Value
            })
            : Ok(new
            {
                message = "Список проектов.",
                projects = _dataShaper.ShapeData(result.Value, include)
            });
    }

    [Idempotent]
    [HttpPost("Create")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CreateProject([FromBody] ProjectCreateDto projectCreateDto)
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(projectCreateDto.Name))
            return BadRequest(new { message = "Название проекта обязательно." });

        var result = await _projectService.CreateProjects(projectCreateDto);

        if (result.IsFailure)
            return NotFound(result.Error);

        return Ok(new { message = $"Проект с Id {result.Value} успешно создана." });
    }

    [HttpPut("Update/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> UpdateProject(Guid id, [FromBody] ProjectUpdateDto projectUpdateDto)
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(projectUpdateDto.Name))
            return BadRequest(new { message = "Название проекта обязательно." });

        var result = await _projectService.UpdateProjects(id, projectUpdateDto);

        if (result.IsFailure)
            return NotFound(result.Error);

        return Ok(new { message = $"Проект с Id {id} успешно обновлена." });
    }

    [HttpDelete("Delete/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> DeleteProject(Guid id)
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
            return Unauthorized();

        var result = await _projectService.DeleteProjects(id);

        if (result.IsFailure)
            return NotFound(result.Error);

        return Ok(new { message = $"Проект с Id {id} успешно удалена." });
    }

    [HttpGet("Get/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetProject(Guid id, [FromQuery] string? include)
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
            return Unauthorized();

        var result = await _projectService.GetProject(id);

        if (result.IsFailure)
            return NotFound(result.Error);

        return string.IsNullOrWhiteSpace(include)
            ? Ok(new
            {
                message = $"Проект с Id {id} найден.",
                project = result.Value
            })
            : Ok(new
            {
                message = $"Проект с Id {id} найден.",
                project = _dataShaper.ShapeData(result.Value, include)
            });
    }
}
