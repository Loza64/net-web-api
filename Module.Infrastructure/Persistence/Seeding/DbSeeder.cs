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

        var dbContext = sp.GetRequiredService<AppDbContext>();
        var db = sp.GetRequiredService<IApplicationDbContext>();
        var unitOfWork = sp.GetRequiredService<IUnitOfWork>();
        var hasher = sp.GetRequiredService<IPasswordHasher>();
        var superAdmin = sp.GetRequiredService<IOptions<SuperAdminSettings>>().Value;

        if (migrate) await dbContext.Database.MigrateAsync(ct);

        await PermissionSeeder.SeedAsync(db, unitOfWork, ct);
        await SuperAdminSeeder.SeedAsync(db, unitOfWork, hasher, superAdmin, ct);
        await RoleSeeder.SeedAsync(db, unitOfWork, ct);
    }
}
