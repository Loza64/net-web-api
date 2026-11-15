using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Module.Application.Contracts.Auth;
using Module.Application.Contracts.Profile;

namespace Module.Test.Integration;

[Trait("Category", "integration")]
public class AuthenticationApiTests : ModuleApiTestBase
{
    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "critical-path")]
    [Trait("Category", "end-to-end")]
    [Trait("Category", "security")]
    public async Task Signup_profile_refresh_and_logout_work_end_to_end()
    {
        // Arrange
        var registration = CreateRegistration();

        // Act
        var auth = await RegisterAsync(registration);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));
        Assert.False(string.IsNullOrWhiteSpace(auth.RefreshToken));

        // Act
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        var profileResponse = await Client.GetAsync("/api/auth/profile");

        // Assert
        Assert.True(profileResponse.IsSuccessStatusCode,
            $"Profile request returned {(int)profileResponse.StatusCode}: {profileResponse.Headers.WwwAuthenticate}");
        var profile = await profileResponse.Content.ReadFromJsonAsync<ProfileResponse>();
        Assert.Equal(registration.Username, profile!.Username);

        // Act
        var profileUpdate = await Client.PutAsJsonAsync("/api/auth/profile",
            new UpdateProfileRequest(registration.Username, "Updated", "Profile", registration.Email));
        var changePassword = await Client.PatchAsJsonAsync("/api/auth/password",
            new ChangePasswordRequest("StrongPassword123", "NewPassword123", "NewPassword123"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, profileUpdate.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, changePassword.StatusCode);

        // Act
        var refreshResponse = await Client.PostAsJsonAsync(
            "/api/auth/refresh", new RefreshRequest(auth.RefreshToken));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);

        // Act
        var loginResponse = await Client.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest(registration.Username, "NewPassword123"));
        var refreshed = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.NotNull(refreshed);

        // Act
        var refreshAfterLogin = await Client.PostAsJsonAsync(
            "/api/auth/refresh", new RefreshRequest(refreshed!.RefreshToken));
        refreshed = await refreshAfterLogin.Content.ReadFromJsonAsync<AuthResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, refreshAfterLogin.StatusCode);
        Assert.NotNull(refreshed);

        // Act
        var logout = await Client.PostAsJsonAsync(
            "/api/auth/logout", new LogoutRequest(refreshed!.RefreshToken));

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        // Act
        var reused = await Client.PostAsJsonAsync(
            "/api/auth/refresh", new RefreshRequest(refreshed.RefreshToken));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, reused.StatusCode);
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "security")]
    public async Task Invalid_signup_is_rejected_and_protected_endpoint_requires_authentication()
    {
        // Arrange
        var invalidRequest = new RegisterRequest("x", "bad-email", "weak", "", "");

        // Act
        var invalidSignup = await Client.PostAsJsonAsync("/api/auth/signup", invalidRequest);
        var protectedRequest = await Client.GetAsync("/api/users");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, invalidSignup.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, protectedRequest.StatusCode);
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "security")]
    public async Task Login_rejects_invalid_password_and_signup_rejects_duplicate_email()
    {
        // Arrange
        var registration = CreateRegistration();
        await RegisterAsync(registration);

        // Act
        var invalidLogin = await Client.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest(registration.Username, "WrongPassword123"));
        var duplicateSignup = await Client.PostAsJsonAsync("/api/auth/signup", registration);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, invalidLogin.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, duplicateSignup.StatusCode);
    }
}
