using TaskSystem.Entities;

namespace TaskSystem.Services.Interfaces;

public interface ITokensService
{
    string GenerateAccessToken(UserEntity userEntity);
}