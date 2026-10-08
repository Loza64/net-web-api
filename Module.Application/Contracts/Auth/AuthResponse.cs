using Module.Application.Contracts.Users;

namespace Module.Application.Contracts.Auth;

public record AuthResponse(string Token, string RefreshToken, UserDetail Data);
