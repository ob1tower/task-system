using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using TaskSystem.Dtos.User;
using TaskSystem.Services.Interfaces;

namespace TaskSystem.Controllers.v1;

[AllowAnonymous]
[ApiController]
[Route("api/v{version:apiVersion}/auth")]
[ApiVersion(1.0)]
public class AuthController : ControllerBase
{
    private readonly IUsersService _usersService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUsersService usersService, ILogger<AuthController> logger)
    {
        _usersService = usersService;
        _logger = logger;
    }

    [HttpPost("Register")]
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
        return Ok(new { message = "User registered successfully." });
    }
}
