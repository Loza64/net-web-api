using Module.Infrastructure.Auth;

namespace Module.Test.Unit;

public class PasswordHasherTests
{
    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "security")]
    public void Hash_and_verify_supports_correct_password_only()
    {
        // Arrange
        var hasher = new BCryptPasswordHasher();
        const string password = "StrongPassword123";

        // Act
        var hash = hasher.Hash(password);

        // Assert
        Assert.NotEqual(password, hash);
        Assert.True(hasher.Verify("StrongPassword123", hash));
        Assert.False(hasher.Verify("DifferentPassword123", hash));
    }
}
