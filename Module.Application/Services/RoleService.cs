using Microsoft.EntityFrameworkCore;
using Module.Application.Abstractions;
using Module.Application.Common;
using Module.Application.Contracts.Common;
using Module.Application.Contracts.Roles;
using Module.Application.Mappings;
using Module.Domain.Constants;
using Module.Domain.Entities;

namespace Module.Application.Services;

public interface IRoleService
{
    Task<PagedResult<RoleSummary>> ListAsync(RoleListQuery query, CancellationToken ct);
    Task<RoleDetail> GetAsync(long id, CancellationToken ct);
    Task<RoleDetail> CreateAsync(CreateRoleRequest request, CancellationToken ct);
    Task<RoleDetail> UpdateAsync(long id, UpdateRoleRequest request, CancellationToken ct);
    Task DeleteAsync(long id, CancellationToken ct);
    Task<RoleDetail> SetPermissionsAsync(long id, SetRolePermissionsRequest request, CancellationToken ct);
}

public class RoleService(IApplicationDbContext db, IPermissionService permissions) : IRoleService
{
    public async Task<PagedResult<RoleSummary>> ListAsync(RoleListQuery q, CancellationToken ct)
    {
        var query = db.Roles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim().ToLowerInvariant();
            query = query.Where(r => r.Name.ToLower().Contains(s));
        }
        if (q.Active is { } active) query = query.Where(r => r.Active == active);

        IOrderedQueryable<Role> ordered = q.SortBy?.ToLowerInvariant() == "name"
            ? (q.Desc ? query.OrderByDescending(r => r.Name) : query.OrderBy(r => r.Name))
            : (q.Desc ? query.OrderByDescending(r => r.Id) : query.OrderBy(r => r.Id));

        return await ordered
            .Select(r => new RoleSummary(r.Id, r.Name, r.Active, r.CreatedAt, r.UpdatedAt, r.DeletedAt))
            .ToPagedAsync(q.Page, q.PageSize, ct);
    }

    public async Task<RoleDetail> GetAsync(long id, CancellationToken ct)
    {
        var role = await db.Roles.AsNoTracking().Include(r => r.Permissions)
                       .FirstOrDefaultAsync(r => r.Id == id, ct)
                   ?? throw AppException.NotFound("Rol no encontrado.");
        return role.ToDetail();
    }

    public async Task<RoleDetail> CreateAsync(CreateRoleRequest request, CancellationToken ct)
    {
        var name = request.Name.Trim();
        EnsureNotReservedName(name);
        if (await db.Roles.AnyAsync(r => r.Name == name, ct))
            throw AppException.Conflict("Ya existe un rol con ese nombre.");

        var role = new Role { Name = name, Active = request.Active };
        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);
        return role.ToDetail();
    }

    public async Task<RoleDetail> UpdateAsync(long id, UpdateRoleRequest request, CancellationToken ct)
    {
        var role = await db.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == id, ct)
                   ?? throw AppException.NotFound("Rol no encontrado.");
        EnsureNotProtected(role);

        var name = request.Name.Trim();
        EnsureNotReservedName(name);
        if (role.Name == RoleNames.Admin && name != RoleNames.Admin)
            throw AppException.Conflict("El rol ADMIN es el rol por defecto del registro y no se puede renombrar.");
        if (await db.Roles.AnyAsync(r => r.Id != id && r.Name == name, ct))
            throw AppException.Conflict("Ya existe un rol con ese nombre.");

        role.Name = name;
        role.Active = request.Active;
        await db.SaveChangesAsync(ct);

        permissions.InvalidateAll();
        return role.ToDetail();
    }

    public async Task DeleteAsync(long id, CancellationToken ct)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == id, ct)
                   ?? throw AppException.NotFound("Rol no encontrado.");
        EnsureNotProtected(role);
        if (role.Name == RoleNames.Admin)
            throw AppException.Conflict("El rol ADMIN es el rol por defecto del registro y no se puede eliminar.");

        if (await db.Users.AnyAsync(u => u.RoleId == id, ct))
            throw AppException.Conflict("No se puede eliminar un rol que tiene usuarios asignados.");

        role.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        permissions.InvalidateAll();
    }

    public async Task<RoleDetail> SetPermissionsAsync(long id, SetRolePermissionsRequest request, CancellationToken ct)
    {
        var role = await db.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == id, ct)
                   ?? throw AppException.NotFound("Rol no encontrado.");
        EnsureNotProtected(role);

        var all = await db.Permissions.ToListAsync(ct);
        var wanted = request.PermissionIds.ToHashSet();

        var missing = wanted.Where(pid => all.All(p => p.Id != pid)).ToList();
        if (missing.Count > 0)
            throw AppException.NotFound($"Permisos inexistentes: {string.Join(", ", missing)}");

        role.Permissions.RemoveAll(p => !wanted.Contains(p.Id));
        foreach (var p in all.Where(p => wanted.Contains(p.Id) && role.Permissions.All(x => x.Id != p.Id)))
            role.Permissions.Add(p);

        await db.SaveChangesAsync(ct);
        permissions.InvalidateAll();
        return role.ToDetail();
    }

    private static void EnsureNotProtected(Role role)
    {
        if (role.Name == RoleNames.SuperAdmin)
            throw AppException.Conflict("El rol SuperAdmin está protegido: siempre tiene todos los permisos y no se puede modificar.");
    }

    private static void EnsureNotReservedName(string name)
    {
        if (string.Equals(name, RoleNames.SuperAdmin, StringComparison.OrdinalIgnoreCase))
            throw AppException.Conflict("El nombre SuperAdmin está reservado.");
    }
}
