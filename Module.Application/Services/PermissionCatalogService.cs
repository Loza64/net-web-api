using Module.Application.Abstractions;
using Module.Application.Common;
using Module.Application.Contracts.Common;
using Module.Application.Contracts.Permissions;

namespace Module.Application.Services;

public interface IPermissionCatalogService
{
    Task<PagedResult<PermissionDto>> ListAsync(PermissionListQuery query, CancellationToken ct);
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

        var ordered = q.SortBy?.ToLowerInvariant() == "title"
            ? (q.Desc ? query.OrderByDescending(p => p.Title) : query.OrderBy(p => p.Title))
            : (q.Desc ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name));

        return await ordered
            .Select(p => new PermissionDto(p.Id, p.Name, p.Title))
            .ToPagedAsync(q.Page, q.PageSize, ct);
    }
}
