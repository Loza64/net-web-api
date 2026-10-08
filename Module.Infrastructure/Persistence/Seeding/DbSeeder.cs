using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Module.Application.Abstractions;

namespace Module.Infrastructure.Persistence.Seeding;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, bool migrate, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<AppDbContext>();
        var hasher = sp.GetRequiredService<IPasswordHasher>();
        var superAdmin = sp.GetRequiredService<IOptions<SuperAdminSettings>>().Value;

        if (migrate) await db.Database.MigrateAsync(ct);

        await PermissionSeeder.SeedAsync(db, ct);
        await SuperAdminSeeder.SeedAsync(db, hasher, superAdmin, ct);
        await RoleSeeder.SeedAsync(db, ct);
    }
}
