using Microsoft.Extensions.Options;
using CSharpFunctionalExtensions;
using TaskSystem.Models;
using TaskSystem.Dtos.User;
using TaskSystem.Services.Interfaces;
using TaskSystem.Repositories.Interfaces;

namespace TaskSystem.Services;

public class UsersService : IUsersService
{
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUsersRepository _usersRepository;
    private readonly ITokensService _tokensService;
    private readonly JwtOptions _jwtOptions;

    public UsersService(IPasswordHasher passwordHasher, IUsersRepository usersRepository,
                       ITokensService tokensService, IOptions<JwtOptions> options)
    {
        _passwordHasher = passwordHasher;
        _usersRepository = usersRepository;
        _tokensService = tokensService;
        _jwtOptions = options.Value;
    }

    public async Task<Result> Register(UserRegisterDto userRegisterDto)
    {
        var userName = await _usersRepository.GetByUserName(userRegisterDto.UserName);

        if (userName != null)
        {
            return Result.Failure("Username already exists.");
        }

        var email = await _usersRepository.GetByEmail(userRegisterDto.Email);

        if (email != null)
        {
            return Result.Failure("Почта уже существует.");
        }

        var hashpassword = _passwordHasher.Generate(userRegisterDto.Password);

        var user = new User(Guid.NewGuid(), userRegisterDto.UserName,
                            userRegisterDto.Email.ToLower().Normalize(), hashpassword);

        await _usersRepository.CreateUsers(user);

        return Result.Success();
    }

    public async Task<Result<TokenDto>> Login(UserLoginDto userLoginDto)
    {
        var email = await _usersRepository.GetByEmail(userLoginDto.Email);

        if (email == null)
        {
            return Result.Failure<TokenDto>("Incorrect email.");
        }

        var user = await _usersRepository.GetByEmail(userLoginDto.Email);

        var password = _passwordHasher.Verify(userLoginDto.Password, user!.PasswordHash);

        if (!password)
        {
            return Result.Failure<TokenDto>("Invalid password.");
        }

        var accessToken = _tokensService.GenerateAccessToken(user);

        var tokenDto = new TokenDto(accessToken);

        return Result.Success(tokenDto);
    }
}
