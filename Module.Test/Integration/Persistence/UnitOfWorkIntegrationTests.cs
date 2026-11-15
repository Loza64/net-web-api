using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Module.Application.Abstractions;
using Module.Domain.Entities;
using Module.Infrastructure.Persistence;

namespace Module.Test.Integration;

[Trait("Category", "integration")]
public class UnitOfWorkIntegrationTests
{
    [Fact]
    [Trait("Category", "positive")]
    public async Task SaveChangesAsync_persists_tracked_changes()
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .UseUpperSnakeCaseNamingConvention()
            .Options;

        await using var db = new AppDbContext(options);
        await db.Database.EnsureCreatedAsync();
        IUnitOfWork unitOfWork = db;
        db.Permissions.Add(new Permission { Name = "users.read", Title = "Read users" });

        // Act
        var affectedEntries = await unitOfWork.SaveChangesAsync();

        // Assert
        Assert.Equal(1, affectedEntries);
        Assert.Equal("Read users", await db.Permissions.Select(permission => permission.Title).SingleAsync());
    }

    [Fact]
    [Trait("Category", "positive")]
    public async Task Registered_interfaces_share_scoped_context_and_persist_changes()
    {
        // Arrange
        using var factory = new Module.Test.Support.ModuleApiFactory();
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var applicationDbContext = services.GetRequiredService<IApplicationDbContext>();
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();

        // Act
        applicationDbContext.Permissions.Add(new Permission { Name = "unitofwork.test", Title = "Unit of work test" });
        var affectedEntries = await unitOfWork.SaveChangesAsync();
        var persistedTitle = await applicationDbContext.Permissions
            .Where(permission => permission.Name == "unitofwork.test")
            .Select(permission => permission.Title)
            .SingleAsync();

        // Assert
        Assert.Same(applicationDbContext, unitOfWork);
        Assert.Equal(1, affectedEntries);
        Assert.Equal("Unit of work test", persistedTitle);
    }
}
