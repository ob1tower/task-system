using Asp.Versioning;
using IdempotentAPI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Security.Claims;
using TaskSystem.Dtos;
using TaskSystem.Dtos.Job;
using TaskSystem.Services.Jobs.Interfaces;
using TaskSystem.Services.Shaper.Interfaces;
using TaskSystem.Types;
using TaskSystem.Validators;

namespace TaskSystem.Controllers.v2;

[Authorize(AuthenticationSchemes = "Access",Roles = $"{RolesType.User}")]
[ApiController]
[Route("api/v{version:apiVersion}/job")]
[Produces("application/json")]
[ApiVersion(2.0)]
public class JobController : ControllerBase
{
    private readonly IJobService _jobService;
    private readonly IDataShaper<JobGetDto> _dataShaper;
    private readonly ILogger<JobController> _logger;

    public JobController(IJobService jobService, IDataShaper<JobGetDto> dataShaper, ILogger<JobController> logger)
    {
        _jobService = jobService;
        _dataShaper = dataShaper;
        _logger = logger;
    }

    [HttpGet("GetAll")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetAllJobs([FromQuery] string? include, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("GetAllJobs request for user: {UserId}, page: {PageNumber}, size: {PageSize}",
            userId, pageNumber, pageSize);

        if (userId == null)
        {
            _logger.LogWarning("Unauthorized access attempt to GetAllJobs");
            return Unauthorized();
        }

        var validator = new PaginationValidator(pageNumber, pageSize);

        var result = await _jobService.GetAllJobs(validator.PageNumber, validator.PageSize);

        if (result.IsFailure)
        {
            _logger.LogWarning("GetAllJobs failed for user {UserId}: {Error}", userId, result.Error);
            return NotFound(result.Error);
        }

        _logger.LogInformation("Successfully retrieved {Count} jobs for user: {UserId}", result.Value.Count, userId);
        return string.IsNullOrWhiteSpace(include)
            ? Ok(new
            {
                message = "Jobs list.",
                notes = result.Value
            })
            : Ok(new
            {
                message = "Jobs list.",
                notes = _dataShaper.ShapeData(result.Value, include)
            });
    }

    [Idempotent]
    [HttpPost("Create")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
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

    [HttpGet("Get/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetJob(Guid id, [FromQuery] string? include)
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("GetJob request for user: {UserId}, job ID: {JobId}", userId, id);

        if (userId == null)
        {
            _logger.LogWarning("Unauthorized access attempt to GetJob");
            return Unauthorized();
        }

        var result = await _jobService.GetJob(id);

        if (result.IsFailure)
        {
            _logger.LogWarning("GetJob failed for user {UserId}, job ID {JobId}: {Error}", userId, id, result.Error);
            return NotFound(result.Error);
        }

        _logger.LogInformation("Successfully retrieved job with ID {JobId} for user: {UserId}", id, userId);
        return string.IsNullOrWhiteSpace(include)
            ? Ok(new
            {
                message = $"Job with Id {id} found.",
                note = result.Value
            })
            : Ok(new
            {
                message = $"Job with Id {id} found.",
                note = _dataShaper.ShapeData(result.Value, include)
            });
    }
}