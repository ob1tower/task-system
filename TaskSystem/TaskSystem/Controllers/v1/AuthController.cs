using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    public AuthController(IUsersService usersService)
    {
        _usersService = usersService;
    }

    [HttpPost("Register")]
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
}
