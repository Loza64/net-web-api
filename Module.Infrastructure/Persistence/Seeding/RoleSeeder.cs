using Microsoft.EntityFrameworkCore;
using Module.Domain.Constants;
using Module.Domain.Entities;

namespace Module.Infrastructure.Persistence.Seeding;

internal static class RoleSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct)
    {
        if (!await db.Roles.AnyAsync(r => r.Name == RoleNames.Admin, ct))
        {
            db.Roles.Add(new Role { Name = RoleNames.Admin });
            await db.SaveChangesAsync(ct);
        }
    }
}
