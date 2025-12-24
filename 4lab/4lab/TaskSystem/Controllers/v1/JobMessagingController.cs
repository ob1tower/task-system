using System.Security.Claims;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using TaskSystem.Dtos.Job;
using TaskSystem.RabbitMq;
using TaskSystem.RabbitMq.Messages;
using TaskSystem.Services.Jobs.Interfaces;
using TaskSystem.Types;

namespace TaskSystem.Controllers.v1;

[Authorize(AuthenticationSchemes = "Access", Roles = $"{RolesType.User}")]
[ApiController]
[Route("api/v{version:apiVersion}/job-mq")] // Different route for RabbitMQ-based operations
[ApiVersion(1.0)]
public class JobMessagingController : ControllerBase
{
    private readonly IJobMessageProducer _messageProducer;
    private readonly ILogger<JobMessagingController> _logger;

    public JobMessagingController(IJobMessageProducer messageProducer, ILogger<JobMessagingController> logger)
    {
        _messageProducer = messageProducer;
        _logger = logger;
    }

    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAllJobs([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("GetAllJobs (messaging) request for user: {UserId}, page: {PageNumber}, size: {PageSize}",
            userId, pageNumber, pageSize);

        if (userId == null)
        {
            _logger.LogWarning("Unauthorized access attempt to GetAllJobs (messaging)");
            return Unauthorized();
        }

        var message = new GetAllJobsMessage
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        _messageProducer.SendGetAllJobsMessage(message);

        _logger.LogInformation("Successfully sent GetAllJobs message to queue for user: {UserId}", userId);
        return Ok(new { message = "Запрос на получение всех задач отправлен в очередь." });
    }

    [HttpPost("Create")]
    public async Task<IActionResult> CreateJob([FromBody] JobCreateDto jobCreateDto)
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("CreateJob (messaging) request for user: {UserId}, title: {Title}", userId, jobCreateDto.Title);

        if (userId == null)
        {
            _logger.LogWarning("Unauthorized access attempt to CreateJob (messaging)");
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(jobCreateDto.Title) || jobCreateDto.ProjectId == Guid.Empty)
        {
            _logger.LogWarning("CreateJob (messaging) failed for user {UserId}: Missing required fields", userId);
            return BadRequest(new { message = "Заполните все обязательные поля." });
        }

        var message = new CreateJobMessage
        {
            JobCreateDto = jobCreateDto
        };

        _messageProducer.SendCreateJobMessage(message);

        _logger.LogInformation("Successfully sent CreateJob message to queue for user: {UserId}, created job: {JobTitle}", userId, jobCreateDto.Title);
        return Ok(new { message = "Запрос на создание задачи отправлен в очередь." });
    }

    [HttpPut("Update/{id:guid}")]
    public async Task<IActionResult> UpdateJob(Guid id, [FromBody] JobUpdateDto jobUpdateDto)
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("UpdateJob (messaging) request for user: {UserId}, job ID: {JobId}, new title: {Title}",
            userId, id, jobUpdateDto.Title);

        if (userId == null)
        {
            _logger.LogWarning("Unauthorized access attempt to UpdateJob (messaging)");
            return Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(jobUpdateDto.Title) || jobUpdateDto.ProjectId == Guid.Empty)
        {
            _logger.LogWarning("UpdateJob (messaging) failed for user {UserId}, job ID {JobId}: Missing required fields", userId, id);
            return BadRequest(new { message = "Заполните все обязательные поля." });
        }

        var message = new UpdateJobMessage
        {
            JobId = id,
            JobUpdateDto = jobUpdateDto
        };

        _messageProducer.SendUpdateJobMessage(message);

        _logger.LogInformation("Successfully sent UpdateJob message to queue for user: {UserId}, job ID: {JobId}", userId, id);
        return Ok(new { message = "Запрос на обновление задачи отправлен в очередь." });
    }

    [HttpDelete("Delete/{id:guid}")]
    public async Task<IActionResult> DeleteJob(Guid id)
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation("DeleteJob (messaging) request for user: {UserId}, job ID: {JobId}", userId, id);

        if (userId == null)
        {
            _logger.LogWarning("Unauthorized access attempt to DeleteJob (messaging)");
            return Unauthorized();
        }

        var message = new DeleteJobMessage
        {
            JobId = id
        };

        _messageProducer.SendDeleteJobMessage(message);

        _logger.LogInformation("Successfully sent DeleteJob message to queue for user: {UserId}, job ID: {JobId}", userId, id);
        return Ok(new { message = "Запрос на удаление задачи отправлен в очередь." });
    }
}