namespace Module.Application.Contracts.Roles;

public record RoleListQuery(int Page = 1, int PageSize = 30, string? Search = null, string? SortBy = null,
    bool Desc = false, bool? Active = null);
