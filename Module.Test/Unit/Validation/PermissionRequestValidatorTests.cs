using Module.Application.Contracts.Permissions;
using Module.Application.Validators.Permissions;

namespace Module.Test.Unit.Validation;

public class PermissionRequestValidatorTests
{
    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public void PermissionListQueryValidator_rejects_search_over_100_characters()
    {
        // Arrange
        var validator = new PermissionListQueryValidator();
        var request = new PermissionListQuery(Search: new string('x', 101));

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(PermissionListQuery.Search));
    }

    [Fact]
    [Trait("Category", "negative")]
    [Trait("Category", "boundary")]
    public void PermissionUpdateRequestValidator_rejects_empty_title()
    {
        // Arrange
        var validator = new PermissionUpdateRequestValidator();
        var request = new PermissionUpdateRequest("");

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(PermissionUpdateRequest.Title));
    }
}
