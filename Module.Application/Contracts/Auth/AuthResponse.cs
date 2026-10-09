using Module.Application.Contracts.Profile;

namespace Module.Application.Contracts.Auth;

public record AuthResponse(string Token, string RefreshToken, ProfileResponse Data);
