using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Module.Application.Contracts.Auth;
using Module.Test.Support;

namespace Module.Test.Integration;

public abstract class ModuleApiTestBase : IDisposable
{
    protected ModuleApiTestBase()
    {
        Factory = new ModuleApiFactory();
        Client = Factory.CreateClient();
    }

    protected ModuleApiFactory Factory { get; }
    protected HttpClient Client { get; }

    protected async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        using var response = await Client.PostAsJsonAsync("/api/auth/signup", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        return auth;
    }

    protected async Task<AuthResponse> LoginAsSuperAdminAsync()
    {
        using var response = await Client.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest("superadmin", "TestAdmin123!"));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return auth;
    }

    protected static RegisterRequest CreateRegistration(
        string name = "Integration", string surname = "User") =>
        new(
            $"user-{Guid.NewGuid():N}"[..20],
            $"{Guid.NewGuid():N}@example.com",
            "StrongPassword123",
            name,
            surname);

    public void Dispose()
    {
        Client.Dispose();
        Factory.Dispose();
    }
}
