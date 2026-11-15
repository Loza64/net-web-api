using Module.Application.Contracts.Common;
using Module.Application.Contracts.Users;
using Module.Application.Validators.Users;

namespace Module.Test.Unit.Validation;

public class UserRequestValidatorTests
{
    [Theory]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    [InlineData(0, 30, nameof(UserListQuery.Page))]
    [InlineData(1, 0, nameof(UserListQuery.PageSize))]
    [InlineData(1, 1501, nameof(UserListQuery.PageSize))]
    public void UserListQueryValidator_rejects_out_of_range_pagination(
        int page, int pageSize, string expectedProperty)
    {
        // Arrange
        var validator = new UserListQueryValidator();
        var query = new UserListQuery(page, pageSize);

        // Act
        var result = validator.Validate(query);

        // Assert
        Assert.Contains(result.Errors, error => error.PropertyName == expectedProperty);
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public void CreateUserRequestValidator_rejects_missing_role()
    {
        // Arrange
        var validator = new CreateUserRequestValidator();
        var request = new CreateUserRequest(
            "user", "user@example.com", "StrongPass1", "Test", "User", new IdRequest(0));

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.Contains(result.Errors, error => error.PropertyName == "Role.Id");
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public void UpdateUserRequestValidator_rejects_missing_role()
    {
        // Arrange
        var validator = new UpdateUserRequestValidator();
        var request = new UpdateUserRequest(
            "valid-user", "Test", "User", false, "user@example.com", new IdRequest(0));

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.Contains(result.Errors, error => error.PropertyName == "Role.Id");
    }
}
