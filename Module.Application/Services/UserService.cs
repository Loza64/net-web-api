using Microsoft.EntityFrameworkCore;
using Module.Application.Abstractions;
using Module.Application.Common;
using Module.Application.Contracts.Common;
using Module.Application.Contracts.Users;
using Module.Application.Mappings;
using Module.Domain.Constants;
using Module.Domain.Entities;

namespace Module.Application.Services;

public interface IUserService
{
    Task<PagedResult<UserSummary>> ListAsync(UserListQuery query, CancellationToken ct);
    Task<UserDetail> GetAsync(long id, CancellationToken ct);
    Task<UserDetail> CreateAsync(CreateUserRequest request, CancellationToken ct);
    Task<UserDetail> UpdateAsync(long id, UpdateUserRequest request, long currentUserId, CancellationToken ct);
    Task DeleteAsync(long id, long currentUserId, CancellationToken ct);
}

public class UserService(IApplicationDbContext db, IPasswordHasher hasher, IPermissionService permissions) : IUserService
{
    public async Task<PagedResult<UserSummary>> ListAsync(UserListQuery q, CancellationToken ct)
    {
        var query = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim().ToLowerInvariant();
            query = query.Where(u => u.Username.ToLower().Contains(s) || u.Email.ToLower().Contains(s)
                                  || u.Name.ToLower().Contains(s) || u.Surname.ToLower().Contains(s));
        }
        if (q.RoleId is { } roleId) query = query.Where(u => u.RoleId == roleId);
        if (q.Blocked is { } blocked) query = query.Where(u => u.Blocked == blocked);

        return await Sort(query, q.SortBy, q.Desc)
            .ThenBy(u => u.Id)
            .Select(u => new UserSummary(u.Id, u.Username, u.Email, u.Name, u.Surname, u.Role.Name, u.Blocked))
            .ToPagedAsync(q.Page, q.PageSize, ct);
    }

    public async Task<UserDetail> GetAsync(long id, CancellationToken ct)
    {
        var user = await db.Users.AsNoTracking().Include(u => u.Role).ThenInclude(r => r.Permissions)
                       .FirstOrDefaultAsync(u => u.Id == id, ct)
                   ?? throw AppException.NotFound("Usuario no encontrado.");
        return user.ToDetail();
    }

    public async Task<UserDetail> CreateAsync(CreateUserRequest request, CancellationToken ct)
    {
        var username = request.Username.Trim().ToLowerInvariant();
        var email = request.Email.Trim().ToLowerInvariant();

        await EnsureUniqueAsync(0, username, email, ct);
        var role = await GetActiveRoleAsync(request.RoleId, ct);

        var user = new User
        {
            Username = username,
            Email = email,
            Password = hasher.Hash(request.Password),
            Name = request.Name.Trim(),
            Surname = request.Surname.Trim(),
            RoleId = role.Id
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        user.Role = role;
        return user.ToDetail();
    }

    public async Task<UserDetail> UpdateAsync(long id, UpdateUserRequest request, long currentUserId, CancellationToken ct)
    {
        var user = await db.Users.Include(u => u.Role).ThenInclude(r => r.Permissions)
                       .FirstOrDefaultAsync(u => u.Id == id, ct)
                   ?? throw AppException.NotFound("Usuario no encontrado.");

        if (id == currentUserId && (request.Blocked || request.RoleId != user.RoleId))
            throw AppException.Forbidden("No puedes bloquearte ni cambiar tu propio rol.");

        var username = request.Username.Trim().ToLowerInvariant();
        var email = request.Email.Trim().ToLowerInvariant();

        if (user.Role.Name == RoleNames.SuperAdmin
            && (request.Blocked || request.RoleId != user.RoleId || username != user.Username || email != user.Email))
            throw AppException.Conflict(
                "El super admin está protegido: no se puede bloquear ni cambiar su rol, usuario o email.");

        await EnsureUniqueAsync(id, username, email, ct);

        var role = request.RoleId == user.RoleId ? user.Role : await GetActiveRoleAsync(request.RoleId, ct);
        var justBlocked = request.Blocked && !user.Blocked;

        user.Username = username;
        user.Email = email;
        user.Name = request.Name.Trim();
        user.Surname = request.Surname.Trim();
        user.Blocked = request.Blocked;
        user.RoleId = role.Id;
        user.Role = role;
        await db.SaveChangesAsync(ct);

        if (justBlocked) await RevokeTokensAsync(id, ct);
        permissions.InvalidateAll();
        return user.ToDetail();
    }

    public async Task DeleteAsync(long id, long currentUserId, CancellationToken ct)
    {
        if (id == currentUserId)
            throw AppException.Forbidden("No puedes eliminar tu propio usuario.");

        var user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id, ct)
                   ?? throw AppException.NotFound("Usuario no encontrado.");

        if (user.Role.Name == RoleNames.SuperAdmin)
            throw AppException.Conflict("El super admin está protegido y no se puede eliminar.");

        user.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        await RevokeTokensAsync(id, ct);
        permissions.InvalidateAll();
    }

    private static IOrderedQueryable<User> Sort(IQueryable<User> q, string? by, bool desc) =>
        by?.ToLowerInvariant() switch
        {
            "username" => desc ? q.OrderByDescending(u => u.Username) : q.OrderBy(u => u.Username),
            "email" => desc ? q.OrderByDescending(u => u.Email) : q.OrderBy(u => u.Email),
            "name" => desc ? q.OrderByDescending(u => u.Name) : q.OrderBy(u => u.Name),
            _ => desc ? q.OrderByDescending(u => u.CreatedAt) : q.OrderBy(u => u.CreatedAt),
        };

    private async Task EnsureUniqueAsync(long excludeId, string username, string email, CancellationToken ct)
    {
        if (await db.Users.AnyAsync(u => u.Id != excludeId && u.Email == email, ct))
            throw AppException.Conflict("El email ya está en uso.");
        if (await db.Users.AnyAsync(u => u.Id != excludeId && u.Username == username, ct))
            throw AppException.Conflict("El nombre de usuario ya está en uso.");
    }

    private async Task<Role> GetActiveRoleAsync(long roleId, CancellationToken ct)
    {
        var role = await db.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == roleId, ct)
                   ?? throw AppException.NotFound("Rol no encontrado.");
        if (!role.Active) throw AppException.Conflict("El rol está inactivo.");
        if (role.Name == RoleNames.SuperAdmin)
            throw AppException.Conflict("El rol SuperAdmin no se puede asignar desde la API.");
        return role;
    }

    private Task<int> RevokeTokensAsync(long userId, CancellationToken ct) =>
        db.RefreshTokens.Where(t => t.UserId == userId && !t.Revoked)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Revoked, true), ct);
}
