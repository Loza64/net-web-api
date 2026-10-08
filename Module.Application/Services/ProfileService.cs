using Microsoft.EntityFrameworkCore;
using Module.Application.Abstractions;
using Module.Application.Common;
using Module.Application.Contracts.Profile;
using Module.Application.Contracts.Users;
using Module.Application.Mappings;
using Module.Domain.Constants;
using Module.Domain.Entities;

namespace Module.Application.Services;

public interface IProfileService
{
    Task<UserDetail> GetAsync(long userId, CancellationToken ct);
    Task<UserDetail> UpdateAsync(long userId, UpdateProfileRequest request, CancellationToken ct);
    Task ChangePasswordAsync(long userId, ChangePasswordRequest request, CancellationToken ct);
}

public class ProfileService(IApplicationDbContext db, IPasswordHasher hasher) : IProfileService
{
    public async Task<UserDetail> GetAsync(long userId, CancellationToken ct) =>
        (await LoadAsync(userId, ct)).ToDetail();

    public async Task<UserDetail> UpdateAsync(long userId, UpdateProfileRequest request, CancellationToken ct)
    {
        var user = await LoadAsync(userId, ct);

        var username = request.Username.Trim().ToLowerInvariant();
        var email = request.Email.Trim().ToLowerInvariant();

        if (user.Role.Name == RoleNames.SuperAdmin && (username != user.Username || email != user.Email))
            throw AppException.Conflict("El usuario y el email del super admin se definen en las variables de entorno.");

        if (await db.Users.AnyAsync(u => u.Id != userId && u.Email == email, ct))
            throw AppException.Conflict("El email ya está en uso.");
        if (await db.Users.AnyAsync(u => u.Id != userId && u.Username == username, ct))
            throw AppException.Conflict("El nombre de usuario ya está en uso.");

        user.Username = username;
        user.Email = email;
        user.Name = request.Name.Trim();
        user.Surname = request.Surname.Trim();
        user.Photo = string.IsNullOrWhiteSpace(request.Photo) ? null : request.Photo.Trim();
        await db.SaveChangesAsync(ct);

        return user.ToDetail();
    }

    public async Task ChangePasswordAsync(long userId, ChangePasswordRequest request, CancellationToken ct)
    {
        var user = await LoadAsync(userId, ct);

        if (user.Role.Name == RoleNames.SuperAdmin)
            throw AppException.Conflict("La contraseña del super admin se define en la variable de entorno SuperAdmin__Password.");

        if (!hasher.Verify(request.CurrentPassword, user.Password))
            throw AppException.BadRequest("La contraseña actual es incorrecta.");

        user.Password = hasher.Hash(request.NewPassword);
        await db.SaveChangesAsync(ct);

        await db.RefreshTokens
            .Where(t => t.UserId == userId && !t.Revoked)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Revoked, true), ct);
    }

    private async Task<User> LoadAsync(long userId, CancellationToken ct)
    {
        var user = await db.Users
                       .Include(u => u.Role).ThenInclude(r => r.Permissions)
                       .FirstOrDefaultAsync(u => u.Id == userId, ct)
                   ?? throw AppException.NotFound("Usuario no encontrado.");
        if (user.Blocked) throw AppException.Forbidden("Usuario bloqueado.");
        return user;
    }
}
