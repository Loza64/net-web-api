using Module.Application.Contracts.Auth;
using Module.Application.Contracts.Profile;
using Module.Application.Validators.Auth;
using Module.Application.Validators.Profile;

namespace Module.Test.Unit.Validation;

public class AuthenticationRequestValidatorTests
{
    [Fact]
    [Trait("Category", "positive")]
    public void RegisterRequestValidator_accepts_valid_registration()
    {
        // Arrange
        var validator = new RegisterRequestValidator();
        var request = new RegisterRequest("test.user", "test@example.com", "StrongPass1", "Test", "User");

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    [InlineData("", "not-an-email", "weak", "", "", nameof(RegisterRequest.Username))]
    [InlineData("ab", "test@example.com", "StrongPass1", "Test", "User", nameof(RegisterRequest.Username))]
    [InlineData("valid-user", "test@example.com", "weak", "Test", "User", nameof(RegisterRequest.Password))]
    public void RegisterRequestValidator_rejects_invalid_registration(
        string username, string email, string password, string name, string surname, string expectedProperty)
    {
        // Arrange
        var validator = new RegisterRequestValidator();
        var request = new RegisterRequest(username, email, password, name, surname);

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.Contains(result.Errors, error => error.PropertyName == expectedProperty);
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public void LoginRequestValidator_requires_username_and_password()
    {
        // Arrange
        var validator = new LoginRequestValidator();
        var request = new LoginRequest("", "");

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(LoginRequest.Username));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(LoginRequest.Password));
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public void Refresh_and_logout_validators_require_token()
    {
        // Arrange
        var refreshValidator = new RefreshRequestValidator();
        var logoutValidator = new LogoutRequestValidator();
        var refreshRequest = new RefreshRequest("");
        var logoutRequest = new LogoutRequest("");

        // Act
        var refreshResult = refreshValidator.Validate(refreshRequest);
        var logoutResult = logoutValidator.Validate(logoutRequest);

        // Assert
        Assert.Contains(refreshResult.Errors, error => error.PropertyName == nameof(RefreshRequest.RefreshToken));
        Assert.Contains(logoutResult.Errors, error => error.PropertyName == nameof(LogoutRequest.RefreshToken));
    }

    [Fact]
    [Trait("Category", "negative")]
    public void ChangePasswordRequestValidator_requires_matching_strong_passwords()
    {
        // Arrange
        var validator = new ChangePasswordRequestValidator();
        var request = new ChangePasswordRequest("OldPassword1", "weak", "not-matching");

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ChangePasswordRequest.NewPassword));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(ChangePasswordRequest.ConfirmNewPassword));
    }
}
