using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
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
    private readonly ILogger<ProjectController> _logger;

    public ProjectController(IProjectService projectService, ILogger<ProjectController> logger)
    {
        _projectService = projectService;
        _logger = logger;
    }

    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAllProjects([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("GetAllProjects request for user: {UserId}, page: {PageNumber}, size: {PageSize}",
            userId, pageNumber, pageSize);

        if (userId == null)
        {
            _logger.LogWarning("Unauthorized access attempt to GetAllProjects");
            return Unauthorized();
        }

        var result = await _projectService.GetAllProjects(pageNumber, pageSize);

        if (result.IsFailure)
        {
            _logger.LogWarning("GetAllProjects failed for user {UserId}: {Error}", userId, result.Error);
            return NotFound(result.Error);
        }

        _logger.LogInformation("Successfully retrieved {Count} projects for user: {UserId}", result.Value.Count, userId);
        return Ok(new { message = "Projects list.", projects = result.Value });
    }

    [HttpPost("Create")]
    public async Task<IActionResult> CreateProject([FromBody] ProjectCreateDto projectCreateDto)
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("CreateProject request for user: {UserId}, name: {Name}", userId, projectCreateDto.Name);

        if (userId == null)
        {
            _logger.LogWarning("Unauthorized access attempt to CreateProject");
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(projectCreateDto.Name))
        {
            _logger.LogWarning("CreateProject failed for user {UserId}: Missing name field", userId);
            return BadRequest(new { message = "Project name is required." });
        }

        var result = await _projectService.CreateProjects(projectCreateDto);

        if (result.IsFailure)
        {
            _logger.LogWarning("CreateProject failed for user {UserId}, name {Name}: {Error}",
                userId, projectCreateDto.Name, result.Error);
            return NotFound(result.Error);
        }

        _logger.LogInformation("Successfully created project with ID {ProjectId} for user: {UserId}", result.Value, userId);
        return Ok(new { message = $"Project with Id {result.Value} created successfully." });
    }

    [HttpPut("Update/{id:guid}")]
    public async Task<IActionResult> UpdateProject(Guid id, [FromBody] ProjectUpdateDto projectUpdateDto)
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("UpdateProject request for user: {UserId}, project ID: {ProjectId}, new name: {Name}",
            userId, id, projectUpdateDto.Name);

        if (userId == null)
        {
            _logger.LogWarning("Unauthorized access attempt to UpdateProject");
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(projectUpdateDto.Name))
        {
            _logger.LogWarning("UpdateProject failed for user {UserId}, project ID {ProjectId}: Missing name field", userId, id);
            return BadRequest(new { message = "Project name is required." });
        }

        var result = await _projectService.UpdateProjects(id, projectUpdateDto);

        if (result.IsFailure)
        {
            _logger.LogWarning("UpdateProject failed for user {UserId}, project ID {ProjectId}: {Error}", userId, id, result.Error);
            return NotFound(result.Error);
        }

        _logger.LogInformation("Successfully updated project with ID {ProjectId} for user: {UserId}", id, userId);
        return Ok(new { message = $"Project with Id {id} updated successfully." });
    }

    [HttpDelete("Delete/{id:guid}")]
    public async Task<IActionResult> DeleteProject(Guid id)
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("DeleteProject request for user: {UserId}, project ID: {ProjectId}", userId, id);

        if (userId == null)
        {
            _logger.LogWarning("Unauthorized access attempt to DeleteProject");
            return Unauthorized();
        }

        var result = await _projectService.DeleteProjects(id);

        if (result.IsFailure)
        {
            _logger.LogWarning("DeleteProject failed for user {UserId}, project ID {ProjectId}: {Error}", userId, id, result.Error);
            return NotFound(result.Error);
        }

        _logger.LogInformation("Successfully deleted project with ID {ProjectId} for user: {UserId}", id, userId);
        return Ok(new { message = $"Project with Id {id} deleted successfully." });
    }
}
