using Module.Application.Contracts.Common;
using Module.Application.Contracts.Roles;
using Module.Application.Validators.Roles;

namespace Module.Test.Unit.Validation;

public class RoleRequestValidatorTests
{
    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public void CreateRoleRequestValidator_rejects_nonpositive_permission_ids()
    {
        // Arrange
        var validator = new CreateRoleRequestValidator();
        var request = new CreateRoleRequest("Support", Permissions: [new IdRequest(0)]);

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.Contains(result.Errors, error => error.PropertyName == "Permissions[0].Id");
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public void RoleListQueryValidator_rejects_page_below_one()
    {
        // Arrange
        var validator = new RoleListQueryValidator();
        var request = new RoleListQuery(Page: 0);

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(RoleListQuery.Page));
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public void SetRolePermissionsRequestValidator_rejects_nonpositive_ids()
    {
        // Arrange
        var validator = new SetRolePermissionsRequestValidator();
        var request = new SetRolePermissionsRequest([0]);

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.Contains(result.Errors, error => error.PropertyName == "PermissionIds[0]");
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public void UpdateRoleRequestValidator_rejects_short_names()
    {
        // Arrange
        var validator = new UpdateRoleRequestValidator();
        var request = new UpdateRoleRequest("x", true);

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateRoleRequest.Name));
    }
}
