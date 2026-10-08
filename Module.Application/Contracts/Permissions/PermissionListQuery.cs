namespace Module.Application.Contracts.Permissions;

public record PermissionListQuery(int Page = 1, int PageSize = 30, string? Search = null,
    string? SortBy = null, bool Desc = false);
