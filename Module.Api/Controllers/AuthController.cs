using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Module.Application.Contracts.Auth;
using Module.Application.Contracts.Profile;
using Module.Application.Contracts.Users;
using Module.Application.Services;
using Module.Api.Authorization;

namespace Module.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService auth, IProfileService profile) : ControllerBase
{
    [AllowAnonymous, EnableRateLimiting("auth")]
    [HttpPost("login")]
    public Task<AuthResponse> Login(LoginRequest request, CancellationToken ct) =>
        auth.LoginAsync(request, ct);

    [AllowAnonymous, EnableRateLimiting("auth")]
    [HttpPost("signup")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await auth.RegisterAsync(request, ct));

    [AllowAnonymous, EnableRateLimiting("auth")]
    [HttpPost("refresh")]
    public Task<AuthResponse> Refresh(RefreshRequest request, CancellationToken ct) =>
        auth.RefreshAsync(request, ct);

    [AllowAnonymous, EnableRateLimiting("auth")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken ct)
    {
        await auth.LogoutAsync(request, ct);
        return NoContent();
    }

    [Authorize]
    [HttpGet("profile")]
    public Task<ProfileResponse> GetProfile(CancellationToken ct) =>
        profile.GetAsync(User.GetUserId(), ct);

    [Authorize]
    [HttpPut("profile")]
    public Task<ProfileResponse> UpdateProfile(UpdateProfileRequest request, CancellationToken ct) =>
        profile.UpdateAsync(User.GetUserId(), request, ct);

    [Authorize]
    [HttpPatch("password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        await profile.ChangePasswordAsync(User.GetUserId(), request, ct);
        return NoContent();
    }
}
