using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using TaskSystem.Dtos.Job;
using TaskSystem.Services.Jobs.Interfaces;
using TaskSystem.Types;

namespace TaskSystem.Controllers.v1;

[Authorize(AuthenticationSchemes = "Access",Roles = $"{RolesType.User}")]
[ApiController]
[Route("api/v{version:apiVersion}/job")]
[ApiVersion(1.0)]
public class JobController : ControllerBase
{
    private readonly IJobService _jobService;
    private readonly ILogger<JobController> _logger;

    public JobController(IJobService jobService, ILogger<JobController> logger)
    {
        _jobService = jobService;
        _logger = logger;
    }

    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAllJobs([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("GetAllJobs request for user: {UserId}, page: {PageNumber}, size: {PageSize}",
            userId, pageNumber, pageSize);

        if (userId == null)
        {
            _logger.LogWarning("Unauthorized access attempt to GetAllJobs");
            return Unauthorized();
        }

        var result = await _jobService.GetAllJobs(pageNumber, pageSize);

        if (result.IsFailure)
        {
            _logger.LogWarning("GetAllJobs failed for user {UserId}: {Error}", userId, result.Error);
            return NotFound(result.Error);
        }

        _logger.LogInformation("Successfully retrieved {Count} jobs for user: {UserId}", result.Value.Count, userId);
        return Ok(new { message = "Jobs list.", notes = result.Value });
    }

    [HttpPost("Create")]
    public async Task<IActionResult> CreateJob([FromBody] JobCreateDto jobCreateDto)
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("CreateJob request for user: {UserId}, title: {Title}", userId, jobCreateDto.Title);

        if (userId == null)
        {
            _logger.LogWarning("Unauthorized access attempt to CreateJob");
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(jobCreateDto.Title) || jobCreateDto.ProjectId == Guid.Empty)
        {
            _logger.LogWarning("CreateJob failed for user {UserId}: Missing required fields", userId);
            return BadRequest(new { message = "Fill in all required fields." });
        }

        var result = await _jobService.CreateJobs(jobCreateDto);

        if (result.IsFailure)
        {
            _logger.LogWarning("CreateJob failed for user {UserId}, title {Title}: {Error}",
                userId, jobCreateDto.Title, result.Error);
            return NotFound(result.Error);
        }

        _logger.LogInformation("Successfully created job with ID {JobId} for user: {UserId}", result.Value, userId);
        return Ok(new { message = $"Job with Id {result.Value} created successfully." });
    }

    [HttpPut("Update/{id:guid}")]
    public async Task<IActionResult> UpdateJob(Guid id, [FromBody] JobUpdateDto jobUpdateDto)
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("UpdateJob request for user: {UserId}, job ID: {JobId}, new title: {Title}",
            userId, id, jobUpdateDto.Title);

        if (userId == null)
        {
            _logger.LogWarning("Unauthorized access attempt to UpdateJob");
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(jobUpdateDto.Title) || jobUpdateDto.ProjectId == Guid.Empty)
        {
            _logger.LogWarning("UpdateJob failed for user {UserId}, job ID {JobId}: Missing required fields", userId, id);
            return BadRequest(new { message = "Fill in all required fields." });
        }

        var result = await _jobService.UpdateJobs(id, jobUpdateDto);

        if (result.IsFailure)
        {
            _logger.LogWarning("UpdateJob failed for user {UserId}, job ID {JobId}: {Error}", userId, id, result.Error);
            return NotFound(result.Error);
        }

        _logger.LogInformation("Successfully updated job with ID {JobId} for user: {UserId}", id, userId);
        return Ok(new { message = $"Job with Id {id} updated successfully." });
    }

    [HttpDelete("Delete/{id:guid}")]
    public async Task<IActionResult> DeleteJob(Guid id)
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("DeleteJob request for user: {UserId}, job ID: {JobId}", userId, id);

        if (userId == null)
        {
            _logger.LogWarning("Unauthorized access attempt to DeleteJob");
            return Unauthorized();
        }

        var result = await _jobService.DeleteJobs(id);

        if (result.IsFailure)
        {
            _logger.LogWarning("DeleteJob failed for user {UserId}, job ID {JobId}: {Error}", userId, id, result.Error);
            return NotFound(result.Error);
        }

        _logger.LogInformation("Successfully deleted job with ID {JobId} for user: {UserId}", id, userId);
        return Ok(new { message = $"Job with Id {id} deleted successfully." });
    }
}