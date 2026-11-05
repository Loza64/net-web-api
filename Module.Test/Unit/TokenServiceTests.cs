using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using Module.Domain.Entities;
using Module.Infrastructure.Auth;

namespace Module.Test.Unit;

public class TokenServiceTests
{
    private static TokenService CreateService() =>
        new(Options.Create(new JwtSettings
        {
            Issuer = "unit-test",
            Audience = "unit-test-client",
            Key = "Unit.Test.Signing.Key.Must.Have.At.Least.32.Characters",
            AccessTokenMinutes = 20,
            RefreshTokenDays = 10
        }));

    [Fact]
    public void CreateAccessToken_includes_user_identity_and_role()
    {
        var service = CreateService();
        var user = new User
        {
            Id = 42,
            Email = "user@example.com",
            Role = new Role { Name = "Admin" }
        };

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(service.CreateAccessToken(user));

        Assert.Equal("42", jwt.Subject);
        Assert.Equal("user@example.com", jwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("Admin", jwt.Claims.Single(c => c.Type == "role").Value);
        Assert.Equal(20 * 60, service.AccessTokenLifetimeSeconds);
    }

    [Fact]
    public void CreateRefreshToken_returns_raw_token_and_stored_hash()
    {
        var service = CreateService();

        var refresh = service.CreateRefreshToken();

        Assert.NotEqual(refresh.Raw, refresh.Hash);
        Assert.Equal(service.HashRefreshToken(refresh.Raw), refresh.Hash);
        Assert.True(refresh.ExpiresAt > DateTime.UtcNow.AddDays(9));
    }
}
