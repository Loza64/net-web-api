using Microsoft.EntityFrameworkCore;
using Module.Application.Contracts.Common;

namespace Module.Application.Common;

public static class QueryableExtensions
{
    public const int MaxPageSize = 100;

    public static async Task<PagedResult<T>> ToPagedAsync<T>(
        this IQueryable<T> query, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(page, 0);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var total = await query.CountAsync(ct);
        var items = await query.Skip(page * pageSize).Take(pageSize).ToListAsync(ct);
        var pageCount = (int)Math.Ceiling(total / (double)pageSize);
        return new PagedResult<T>(items, new Pagination(page, pageSize, pageCount, total));
    }
}
