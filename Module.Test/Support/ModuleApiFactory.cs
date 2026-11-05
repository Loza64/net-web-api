using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Module.Infrastructure.Auth;
using Module.Infrastructure.Persistence;

namespace Module.Test.Support;

public sealed class ModuleApiFactory : WebApplicationFactory<Program>
{
    private const string JwtKey = "Module.Test.Signing.Key.Must.Have.At.Least.32.Characters";
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public ModuleApiFactory()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .UseUpperSnakeCaseNamingConvention()
            .Options;

        using var db = new AppDbContext(options);
        db.Database.EnsureCreated();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _connection.ConnectionString,
                ["Jwt:Issuer"] = "module-test",
                ["Jwt:Audience"] = "module-test-client",
                ["Jwt:Key"] = JwtKey,
                ["Jwt:AccessTokenMinutes"] = "15",
                ["Jwt:RefreshTokenDays"] = "7",
                ["SuperAdmin:Email"] = "superadmin@module.test",
                ["SuperAdmin:Username"] = "superadmin",
                ["SuperAdmin:Password"] = "TestAdmin123!",
                ["SuperAdmin:Name"] = "Test",
                ["SuperAdmin:Surname"] = "Admin"
            }));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.AddDbContext<AppDbContext>(options => options
                .UseSqlite(_connection)
                .UseUpperSnakeCaseNamingConvention());
            services.PostConfigure<JwtSettings>(options =>
            {
                options.Issuer = "module-test";
                options.Audience = "module-test-client";
                options.Key = JwtKey;
                options.AccessTokenMinutes = 15;
                options.RefreshTokenDays = 7;
            });
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters.ValidIssuer = "module-test";
                options.TokenValidationParameters.ValidAudience = "module-test-client";
                options.TokenValidationParameters.IssuerSigningKey =
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }
}
