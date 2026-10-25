using Microsoft.EntityFrameworkCore;
using Module.Application.Abstractions;
using Module.Application.Common;
using Module.Application.Contracts.Common;
using Module.Application.Contracts.Permissions;
using Module.Application.Mappings;

namespace Module.Application.Services;

public interface IPermissionCatalogService
{
    Task<PagedResult<PermissionDto>> ListAsync(PermissionListQuery query, CancellationToken ct);
    Task<PermissionDto> UpdateAsync(long id, string title, CancellationToken ct);
}

public class PermissionCatalogService(IApplicationDbContext db) : IPermissionCatalogService
{
    public async Task<PagedResult<PermissionDto>> ListAsync(PermissionListQuery q, CancellationToken ct)
    {
        var query = db.Permissions.AsQueryable();

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim().ToLowerInvariant();
            query = query.Where(p => p.Name.ToLower().Contains(s) || p.Title.ToLower().Contains(s));
        }

        var (sortBy, desc) = GetSort(q.Sort);
        var ordered = sortBy.Equals("title", StringComparison.OrdinalIgnoreCase)
            ? (desc ? query.OrderByDescending(p => p.Title) : query.OrderBy(p => p.Title))
            : (desc ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name));

        return await ordered
            .Select(p => new PermissionDto(p.Id, p.Name, p.Title))
            .ToPagedAsync(q.Page, q.PageSize, ct);
    }

    public async Task<PermissionDto> UpdateAsync(long id, string title, CancellationToken ct)
    {
        var permission = await db.Permissions.FirstOrDefaultAsync(p => p.Id == id, ct)
                         ?? throw AppException.NotFound("Permiso no encontrado.");
        permission.Title = title.Trim();
        await db.SaveChangesAsync(ct);
        return permission.ToDto();
    }

    private static (string By, bool Desc) GetSort(string[]? sort)
    {
        var value = sort?.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(value))
            return ("name", false);

        var parts = value.Split(',', StringSplitOptions.TrimEntries);
        return (parts[0], parts.Length == 2 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase));
    }
}
