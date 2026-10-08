using Module.Domain.Entities;

namespace Module.Application.Abstractions;

public record RefreshTokenData(string Raw, string Hash, DateTime ExpiresAt);

public interface ITokenService
{
    int AccessTokenLifetimeSeconds { get; }
    string CreateAccessToken(User user);
    RefreshTokenData CreateRefreshToken();
    string HashRefreshToken(string raw);
}
