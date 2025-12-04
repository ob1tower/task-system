using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskSystem.Dtos.Project;
using TaskSystem.Services.Projects.Interfaces;
using TaskSystem.Types;

namespace TaskSystem.Controllers.v1;

[Authorize(AuthenticationSchemes = "Access", Roles = $"{RolesType.User}")]
[ApiController]
[Route("api/v{version:apiVersion}/project")]
[ApiVersion(1.0)]
public class ProjectController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAllProjects([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
            return Unauthorized();

        var result = await _projectService.GetAllProjects(pageNumber, pageSize);

        if (result.IsFailure)
            return NotFound(result.Error);

        return Ok(new { message = "Список проектов.", projects = result.Value });
    }

    [HttpPost("Create")]
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
}
