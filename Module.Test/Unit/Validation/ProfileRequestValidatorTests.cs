using Module.Application.Contracts.Profile;
using Module.Application.Validators.Profile;

namespace Module.Test.Unit.Validation;

public class ProfileRequestValidatorTests
{
    [Fact]
    [Trait("Category", "negative")]
    public void UpdateProfileRequestValidator_rejects_invalid_email()
    {
        // Arrange
        var validator = new UpdateProfileRequestValidator();
        var request = new UpdateProfileRequest("valid-user", "Test", "User", "invalid-email");

        // Act
        var result = validator.Validate(request);

        // Assert
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(UpdateProfileRequest.Email));
    }
}
