using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    public JobController(IJobService jobService)
    {
        _jobService = jobService;
    }

    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAllJobs([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
            return Unauthorized();

        var result = await _jobService.GetAllJobs(pageNumber, pageSize);

        return result.IsFailure
            ? NotFound(result.Error)
            : Ok(new { message = "Список задач.", notes = result.Value });
    }

    [HttpPost("Create")]
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
}