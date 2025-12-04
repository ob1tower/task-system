using Asp.Versioning;
using IdempotentAPI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    public JobController(IJobService jobService, IDataShaper<JobGetDto> dataShaper)
    {
        _jobService = jobService;
        _dataShaper = dataShaper;
    }

    [HttpGet("GetAll")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetAllJobs([FromQuery] string? include, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
            return Unauthorized();

        var validator = new PaginationValidator(pageNumber, pageSize);

        var result = await _jobService.GetAllJobs(validator.PageNumber, validator.PageSize);

        if (result.IsFailure)
            return NotFound(result.Error);

        return string.IsNullOrWhiteSpace(include)
            ? Ok(new 
            { 
                message = "Список задач.", 
                notes = result.Value 
            })
            : Ok(new 
            {
                message = "Список задач.", 
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

        if (userId == null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(jobCreateDto.Title) || jobCreateDto.ProjectId == Guid.Empty)
            return BadRequest(new { message = "Заполните все обязательные поля." });

        var result = await _jobService.CreateJobs(jobCreateDto);

        return result.IsFailure
            ? NotFound(result.Error)
            : Ok(new { message = $"Задача с Id {result.Value} успешно создана." });
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

        if (userId == null)
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(jobUpdateDto.Title) || jobUpdateDto.ProjectId == Guid.Empty)
            return BadRequest(new { message = "Заполните все обязательные поля." });

        var result = await _jobService.UpdateJobs(id, jobUpdateDto);

        return result.IsFailure
            ? NotFound(result.Error)
            : Ok(new { message = $"Задача с Id {id} успешно обновлена." });
    }

    [HttpDelete("Delete/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> DeleteJob(Guid id)
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
            return Unauthorized();

        var result = await _jobService.DeleteJobs(id);

        return result.IsFailure
            ? NotFound(result.Error)
            : Ok(new { message = $"Задача с Id {id} успешно удалена." });
    }

    [HttpGet("Get/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetJob(Guid id, [FromQuery] string? include)
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
            return Unauthorized();

        var result = await _jobService.GetJob(id);

        if (result.IsFailure)
            return NotFound(result.Error);

        return string.IsNullOrWhiteSpace(include)
            ? Ok(new
            {
                message = $"Задача с Id {id} найдена.",
                note = result.Value
            })
            : Ok(new
            {
                message = $"Задача с Id {id} найдена.",
                note = _dataShaper.ShapeData(result.Value, include)
            });
    }
}