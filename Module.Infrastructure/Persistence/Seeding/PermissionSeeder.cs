using Microsoft.EntityFrameworkCore;
using Module.Domain.Constants;
using Module.Domain.Entities;

namespace Module.Infrastructure.Persistence.Seeding;

internal static class PermissionSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct)
    {
        var existing = await db.Permissions.ToListAsync(ct);

        foreach (var (name, title) in PermissionNames.All)
        {
            var permission = existing.FirstOrDefault(p => p.Name == name);
            if (permission is null)
                db.Permissions.Add(new Permission { Name = name, Title = title });
            else if (permission.Title != title)
                permission.Title = title;
        }

        await db.SaveChangesAsync(ct);
    }
}
