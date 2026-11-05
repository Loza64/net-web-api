using Module.Infrastructure.Auth;

namespace Module.Test.Unit;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_and_verify_supports_correct_password_only()
    {
        var hasher = new BCryptPasswordHasher();

        var hash = hasher.Hash("StrongPassword123");

        Assert.NotEqual("StrongPassword123", hash);
        Assert.True(hasher.Verify("StrongPassword123", hash));
        Assert.False(hasher.Verify("DifferentPassword123", hash));
    }
}
