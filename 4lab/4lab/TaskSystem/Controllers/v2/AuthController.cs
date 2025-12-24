using Asp.Versioning;
using IdempotentAPI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using TaskSystem.Dtos;
using TaskSystem.Dtos.User;
using TaskSystem.Services.Interfaces;

namespace TaskSystem.Controllers.v2;

[AllowAnonymous]
[ApiController]
[Route("api/v{version:apiVersion}/auth")]
[Produces("application/json")]
[ApiVersion(2.0)]
public class AuthController : ControllerBase
{
    private readonly IUsersService _usersService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUsersService usersService, ILogger<AuthController> logger)
    {
        _usersService = usersService;
        _logger = logger;
    }

    [Idempotent]
    [HttpPost("Register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register([FromBody] UserRegisterDto userRegisterDto)
    {
        _logger.LogInformation("Registration attempt for user: {UserName}, email: {Email}",
            userRegisterDto.UserName, userRegisterDto.Email);

        if (string.IsNullOrWhiteSpace(userRegisterDto.UserName) ||
            string.IsNullOrWhiteSpace(userRegisterDto.Email) ||
            string.IsNullOrWhiteSpace(userRegisterDto.Password))
        {
            _logger.LogWarning("Registration failed: Missing required fields for user: {UserName}", userRegisterDto.UserName);
            return BadRequest(new { message = "Заполните все обязательные поля." });
        }

        var result = await _usersService.Register(userRegisterDto);

        if (result.IsFailure)
        {
            _logger.LogWarning("Registration failed for user {UserName}: {Error}",
                userRegisterDto.UserName, result.Error);
            return Conflict(new { message = result.Error });
        }

        _logger.LogInformation("Successfully registered user: {UserName}", userRegisterDto.UserName);
        return Ok(new { message = "Пользователь успешно зарегистрирован." });
    }

    [HttpPost("Login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] UserLoginDto userLoginDto)
    {
        _logger.LogInformation("Login attempt for email: {Email}", userLoginDto.Email);

        if (string.IsNullOrWhiteSpace(userLoginDto.Email) ||
            string.IsNullOrWhiteSpace(userLoginDto.Password))
        {
            _logger.LogWarning("Login failed: Missing required fields for email: {Email}", userLoginDto.Email);
            return BadRequest(new { message = "Заполните все обязательные поля." });
        }

        var result = await _usersService.Login(userLoginDto);

        if (result.IsFailure)
        {
            _logger.LogWarning("Login failed for email {Email}: {Error}", userLoginDto.Email, result.Error);
            return Unauthorized(new { message = result.Error });
        }

        _logger.LogInformation("Successfully logged in user: {Email}", userLoginDto.Email);
        return Ok(new { message = "Пользователь успешно авторизован.", tokens = result.Value });
    }
}
