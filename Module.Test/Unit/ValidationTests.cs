using Module.Application.Contracts.Auth;
using Module.Application.Contracts.Common;
using Module.Application.Contracts.Permissions;
using Module.Application.Contracts.Profile;
using Module.Application.Contracts.Roles;
using Module.Application.Contracts.Users;
using Module.Application.Validators.Auth;
using Module.Application.Validators.Permissions;
using Module.Application.Validators.Profile;
using Module.Application.Validators.Roles;
using Module.Application.Validators.Users;

namespace Module.Test.Unit;

public class ValidationTests
{
    [Fact]
    public void RegisterRequestValidator_accepts_valid_registration()
    {
        var result = new RegisterRequestValidator().Validate(
            new RegisterRequest("test.user", "test@example.com", "StrongPass1", "Test", "User"));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "not-an-email", "weak", "", "")]
    [InlineData("ab", "test@example.com", "StrongPass1", "Test", "User")]
    [InlineData("valid-user", "test@example.com", "weak", "Test", "User")]
    public void RegisterRequestValidator_rejects_invalid_registration(
        string username, string email, string password, string name, string surname)
    {
        var result = new RegisterRequestValidator().Validate(
            new RegisterRequest(username, email, password, name, surname));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void LoginRequestValidator_requires_username_and_password()
    {
        var result = new LoginRequestValidator().Validate(new LoginRequest("", ""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(LoginRequest.Username));
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(LoginRequest.Password));
    }

    [Theory]
    [InlineData(0, 30)]
    [InlineData(1, 0)]
    [InlineData(1, 1501)]
    public void UserListQueryValidator_rejects_out_of_range_pagination(int page, int pageSize)
    {
        var result = new UserListQueryValidator().Validate(new UserListQuery(page, pageSize));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CreateRoleRequestValidator_rejects_nonpositive_permission_ids()
    {
        var result = new CreateRoleRequestValidator().Validate(
            new CreateRoleRequest("Support", Permissions: [new IdRequest(0)]));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CreateUserRequestValidator_rejects_missing_role()
    {
        var request = new CreateUserRequest(
            "user", "user@example.com", "StrongPass1", "Test", "User", new IdRequest(0));

        Assert.False(new CreateUserRequestValidator().Validate(request).IsValid);
    }

    [Fact]
    public void PermissionListQueryValidator_rejects_search_over_100_characters()
    {
        var request = new PermissionListQuery(Search: new string('x', 101));

        Assert.False(new PermissionListQueryValidator().Validate(request).IsValid);
    }

    [Fact]
    public void PermissionUpdateRequestValidator_rejects_empty_title()
    {
        var request = new PermissionUpdateRequest("");

        Assert.False(new PermissionUpdateRequestValidator().Validate(request).IsValid);
    }

    [Fact]
    public void Refresh_and_logout_validators_require_token()
    {
        Assert.False(new RefreshRequestValidator().Validate(new RefreshRequest("")).IsValid);
        Assert.False(new LogoutRequestValidator().Validate(new LogoutRequest("")).IsValid);
    }

    [Fact]
    public void ChangePasswordRequestValidator_requires_matching_strong_passwords()
    {
        var request = new ChangePasswordRequest("OldPassword1", "weak", "not-matching");

        Assert.False(new ChangePasswordRequestValidator().Validate(request).IsValid);
    }

    [Fact]
    public void UpdateProfileRequestValidator_rejects_invalid_email()
    {
        var request = new UpdateProfileRequest("valid-user", "Test", "User", "invalid-email");

        Assert.False(new UpdateProfileRequestValidator().Validate(request).IsValid);
    }

    [Fact]
    public void RoleListQueryValidator_rejects_page_below_one()
    {
        var request = new RoleListQuery(Page: 0);

        Assert.False(new RoleListQueryValidator().Validate(request).IsValid);
    }

    [Fact]
    public void SetRolePermissionsRequestValidator_rejects_nonpositive_ids()
    {
        var request = new SetRolePermissionsRequest([0]);

        Assert.False(new SetRolePermissionsRequestValidator().Validate(request).IsValid);
    }

    [Fact]
    public void UpdateUserRequestValidator_rejects_missing_role()
    {
        var request = new UpdateUserRequest(
            "valid-user", "Test", "User", false, "user@example.com", new IdRequest(0));

        Assert.False(new UpdateUserRequestValidator().Validate(request).IsValid);
    }

    [Fact]
    public void UpdateRoleRequestValidator_rejects_short_names()
    {
        var request = new UpdateRoleRequest("x", true);

        Assert.False(new UpdateRoleRequestValidator().Validate(request).IsValid);
    }
}
