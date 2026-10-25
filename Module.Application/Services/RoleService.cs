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
    Task<PagedResult<RoleDetail>> ListAsync(RoleListQuery query, CancellationToken ct);
    Task<RoleDetail> GetAsync(long id, CancellationToken ct);
    Task<RoleDetail> CreateAsync(CreateRoleRequest request, CancellationToken ct);
    Task<RoleDetail> UpdateAsync(long id, UpdateRoleRequest request, CancellationToken ct);
    Task DeleteAsync(long id, CancellationToken ct);
    Task RestoreAsync(long id, CancellationToken ct);
    Task<RoleDetail> SetPermissionsAsync(long id, SetRolePermissionsRequest request, CancellationToken ct);
}

public class RoleService(IApplicationDbContext db, IPermissionService permissions) : IRoleService
{
    public async Task<PagedResult<RoleDetail>> ListAsync(RoleListQuery q, CancellationToken ct)
    {
        var query = db.Roles.AsNoTracking();
        if (q.Deleted)
            query = query.IgnoreQueryFilters().Where(r => r.DeletedAt != null);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim().ToLowerInvariant();
            query = query.Where(r => r.Name.ToLower().Contains(s));
        }
        if (q.Active is { } active) query = query.Where(r => r.Active == active);

        var (sortBy, desc) = GetSort(q.Sort);
        IOrderedQueryable<Role> ordered = sortBy.Equals("name", StringComparison.OrdinalIgnoreCase)
            ? (desc ? query.OrderByDescending(r => r.Name) : query.OrderBy(r => r.Name))
            : (desc ? query.OrderByDescending(r => r.Id) : query.OrderBy(r => r.Id));

        var page = await ordered.Include(r => r.Permissions).ToPagedAsync(q.Page, q.PageSize, ct);
        return new PagedResult<RoleDetail>(page.Data.Select(r => r.ToDetail()).ToList(), page.Pagination);
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
        await SyncPermissionsAsync(role, request.Permissions ?? [], ct);
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
        if (request.Permissions is not null)
            await SyncPermissionsAsync(role, request.Permissions, ct);
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

    public async Task RestoreAsync(long id, CancellationToken ct)
    {
        var role = await db.Roles.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == id, ct)
                   ?? throw AppException.NotFound("Rol no encontrado.");
        if (role.DeletedAt is null)
            return;
        if (await db.Roles.AnyAsync(r => r.Id != id && r.Name == role.Name, ct))
            throw AppException.Conflict("Ya existe un rol activo con ese nombre.");

        role.DeletedAt = null;
        await db.SaveChangesAsync(ct);
        permissions.InvalidateAll();
    }

    public async Task<RoleDetail> SetPermissionsAsync(long id, SetRolePermissionsRequest request, CancellationToken ct)
    {
        var role = await db.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == id, ct)
                   ?? throw AppException.NotFound("Rol no encontrado.");
        EnsureNotProtected(role);

        await SyncPermissionsAsync(role, request.PermissionIds.Select(id => new IdRequest(id)).ToList(), ct);

        await db.SaveChangesAsync(ct);
        permissions.InvalidateAll();
        return role.ToDetail();
    }

    private async Task SyncPermissionsAsync(Role role, IReadOnlyList<IdRequest> permissionIds, CancellationToken ct)
    {
        var wanted = permissionIds.Select(permission => permission.Id).ToHashSet();
        var all = await db.Permissions.Where(permission => wanted.Contains(permission.Id)).ToListAsync(ct);
        var missing = wanted.Except(all.Select(permission => permission.Id)).ToList();
        if (missing.Count > 0)
            throw AppException.NotFound($"Permisos inexistentes: {string.Join(", ", missing)}");

        role.Permissions.RemoveAll(permission => !wanted.Contains(permission.Id));
        foreach (var permission in all.Where(permission =>
                     role.Permissions.All(existing => existing.Id != permission.Id)))
            role.Permissions.Add(permission);
    }

    private static (string By, bool Desc) GetSort(string[]? sort)
    {
        var value = sort?.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(value))
            return ("id", false);

        var parts = value.Split(',', StringSplitOptions.TrimEntries);
        return (parts[0], parts.Length == 2 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase));
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
