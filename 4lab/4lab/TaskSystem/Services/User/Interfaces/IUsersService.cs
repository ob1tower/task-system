using CSharpFunctionalExtensions;
using TaskSystem.Dtos.User;

namespace TaskSystem.Services.Interfaces;

public interface IUsersService
{
    Task<Result<TokenDto>> Login(UserLoginDto userLoginDto);
    Task<Result> Register(UserRegisterDto userRegisterDto);
}