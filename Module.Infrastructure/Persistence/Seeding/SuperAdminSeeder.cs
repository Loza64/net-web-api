using Microsoft.EntityFrameworkCore;
using Module.Application.Abstractions;
using Module.Domain.Constants;
using Module.Domain.Entities;

namespace Module.Infrastructure.Persistence.Seeding;

internal static class SuperAdminSeeder
{
    public static async Task SeedAsync(
        IApplicationDbContext db, IUnitOfWork unitOfWork, IPasswordHasher hasher,
        SuperAdminSettings settings, CancellationToken ct)
    {
        var role = await EnsureRoleAsync(db, unitOfWork, ct);
        await EnsureUserAsync(db, unitOfWork, hasher, settings, role, ct);
    }

    private static async Task<Role> EnsureRoleAsync(
        IApplicationDbContext db, IUnitOfWork unitOfWork, CancellationToken ct)
    {
        var role = await db.Roles.Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.Name == RoleNames.SuperAdmin, ct);

        if (role is null)
        {
            role = new Role { Name = RoleNames.SuperAdmin };
            db.Roles.Add(role);
        }
        role.Active = true;

        var granted = role.Permissions.Select(p => p.Id).ToHashSet();
        var all = await db.Permissions.ToListAsync(ct);
        foreach (var permission in all.Where(p => !granted.Contains(p.Id)))
            role.Permissions.Add(permission);

        await unitOfWork.SaveChangesAsync(ct);
        return role;
    }

    private static async Task EnsureUserAsync(
        IApplicationDbContext db, IUnitOfWork unitOfWork, IPasswordHasher hasher,
        SuperAdminSettings settings, Role role, CancellationToken ct)
    {
        var email = settings.Email.Trim().ToLowerInvariant();
        var username = settings.Username.Trim().ToLowerInvariant();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null)
        {
            if (await db.Users.AnyAsync(u => u.Username == username, ct))
                throw new InvalidOperationException(
                    $"No se pudo crear el super admin: el username '{username}' ya lo usa otro usuario. " +
                    "Cambia SuperAdmin__Username.");

            db.Users.Add(new User
            {
                Username = username,
                Email = email,
                Password = hasher.Hash(settings.Password),
                Name = settings.Name.Trim(),
                Surname = settings.Surname.Trim(),
                RoleId = role.Id
            });
            await unitOfWork.SaveChangesAsync(ct);
            return;
        }

        var passwordChanged = !hasher.Verify(settings.Password, user.Password);

        user.RoleId = role.Id;
        user.Blocked = false;
        if (passwordChanged) user.Password = hasher.Hash(settings.Password);
        await unitOfWork.SaveChangesAsync(ct);

        if (passwordChanged)
            await db.RefreshTokens
                .Where(t => t.UserId == user.Id && !t.Revoked)
                .ExecuteUpdateAsync(set => set.SetProperty(t => t.Revoked, true), ct);
    }
}
