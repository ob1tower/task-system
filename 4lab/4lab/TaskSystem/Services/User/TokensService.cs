using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TaskSystem.Entities;
using TaskSystem.Services.Interfaces;

namespace TaskSystem.Services;

public class TokensService : ITokensService
{
    private readonly JwtOptions _options;

    public TokensService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateAccessToken(UserEntity userEntity)
    {
        Claim[] claims =
        [
            new (ClaimTypes.NameIdentifier, userEntity.UserId.ToString()),
            new (JwtRegisteredClaimNames.Email, userEntity.Email),
            new Claim(ClaimTypes.Role, userEntity.RoleId.ToString())
        ];

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey)),
            SecurityAlgorithms.HmacSha256);

        var accessToken = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(_options.AccessTokenExpires),
            signingCredentials: signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(accessToken);
    }
}