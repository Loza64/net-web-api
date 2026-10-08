using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Module.Application.Abstractions;
using Module.Domain.Entities;

namespace Module.Infrastructure.Auth;

public class TokenService(IOptions<JwtSettings> options) : ITokenService
{
    private readonly JwtSettings _s = options.Value;

    public int AccessTokenLifetimeSeconds => _s.AccessTokenMinutes * 60;

    public string CreateAccessToken(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("role", user.Role.Name)
        };

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_s.Key)), SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(_s.Issuer, _s.Audience, claims,
            expires: DateTime.UtcNow.AddMinutes(_s.AccessTokenMinutes), signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    public RefreshTokenData CreateRefreshToken()
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        return new RefreshTokenData(raw, HashRefreshToken(raw), DateTime.UtcNow.AddDays(_s.RefreshTokenDays));
    }

    public string HashRefreshToken(string raw) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
}
