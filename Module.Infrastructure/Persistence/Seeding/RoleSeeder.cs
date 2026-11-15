using Microsoft.EntityFrameworkCore;
using Module.Application.Abstractions;
using Module.Domain.Constants;
using Module.Domain.Entities;

namespace Module.Infrastructure.Persistence.Seeding;

internal static class RoleSeeder
{
    public static async Task SeedAsync(IApplicationDbContext db, IUnitOfWork unitOfWork, CancellationToken ct)
    {
        if (!await db.Roles.AnyAsync(r => r.Name == RoleNames.Admin, ct))
        {
            db.Roles.Add(new Role { Name = RoleNames.Admin });
            await unitOfWork.SaveChangesAsync(ct);
        }
    }
}
