using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Module.Application.Contracts.Auth;
using Module.Application.Contracts.Profile;

namespace Module.Test.Integration;

public class AuthenticationApiTests : ModuleApiTestBase
{
    [Fact]
    public async Task Signup_profile_refresh_and_logout_work_end_to_end()
    {
        var registration = CreateRegistration();
        var auth = await RegisterAsync(registration);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));
        Assert.False(string.IsNullOrWhiteSpace(auth.RefreshToken));

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        var profileResponse = await Client.GetAsync("/api/auth/profile");
        Assert.True(profileResponse.IsSuccessStatusCode,
            $"Profile request returned {(int)profileResponse.StatusCode}: {profileResponse.Headers.WwwAuthenticate}");
        var profile = await profileResponse.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.Equal(registration.Username, profile!.Username);

        var profileUpdate = await Client.PutAsJsonAsync("/api/auth/profile",
            new UpdateProfileRequest(registration.Username, "Updated", "Profile", registration.Email));
        Assert.Equal(HttpStatusCode.OK, profileUpdate.StatusCode);

        var changePassword = await Client.PatchAsJsonAsync("/api/auth/password",
            new ChangePasswordRequest("StrongPassword123", "NewPassword123", "NewPassword123"));
        Assert.Equal(HttpStatusCode.NoContent, changePassword.StatusCode);

        var refreshResponse = await Client.PostAsJsonAsync(
            "/api/auth/refresh", new RefreshRequest(auth.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);

        var loginResponse = await Client.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest(registration.Username, "NewPassword123"));
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var refreshed = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(refreshed);

        var refreshAfterLogin = await Client.PostAsJsonAsync(
            "/api/auth/refresh", new RefreshRequest(refreshed.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, refreshAfterLogin.StatusCode);
        refreshed = await refreshAfterLogin.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(refreshed);

        var logout = await Client.PostAsJsonAsync(
            "/api/auth/logout", new LogoutRequest(refreshed.RefreshToken));
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var reused = await Client.PostAsJsonAsync(
            "/api/auth/refresh", new RefreshRequest(refreshed.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, reused.StatusCode);
    }

    [Fact]
    public async Task Invalid_signup_is_rejected_and_protected_endpoint_requires_authentication()
    {
        var invalidSignup = await Client.PostAsJsonAsync(
            "/api/auth/signup", new RegisterRequest("x", "bad-email", "weak", "", ""));
        var protectedRequest = await Client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.BadRequest, invalidSignup.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, protectedRequest.StatusCode);
    }
}
