namespace Module.Application.Contracts.Roles;

public record RoleListQuery(int Page = 0, int PageSize = 30, string? Search = null,
    bool Deleted = false, string[]? Sort = null, bool? Active = null);
