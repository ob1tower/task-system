using Asp.Versioning;
using IdempotentAPI.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    public AuthController(IUsersService usersService)
    {
        _usersService = usersService;
    }

    [Idempotent]
    [HttpPost("Register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register([FromBody] UserRegisterDto userRegisterDto)
    {
        if (string.IsNullOrWhiteSpace(userRegisterDto.UserName) ||
            string.IsNullOrWhiteSpace(userRegisterDto.Email) ||
            string.IsNullOrWhiteSpace(userRegisterDto.Password))
            return BadRequest(new { message = "Заполните все обязательные поля." });

        var result = await _usersService.Register(userRegisterDto);

        if (result.IsFailure)
            return Conflict(new { message = result.Error });

        return Ok(new { message = "Пользователь успешно зарегистрирован." });
    }

    [HttpPost("Login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ExceptionResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] UserLoginDto userLoginDto)
    {
        if (string.IsNullOrWhiteSpace(userLoginDto.Email) ||
            string.IsNullOrWhiteSpace(userLoginDto.Password))
            return BadRequest(new { message = "Заполните все обязательные поля." });

        var result = await _usersService.Login(userLoginDto);

        if (result.IsFailure)
            return Unauthorized(new { message = result.Error });

        return Ok(new { message = "Пользователь успешно авторизован.", tokens = result.Value });
    }
}
