namespace Module.Application.Contracts.Users;

public record UserListQuery(int Page = 1, int PageSize = 30, string? Search = null, string? SortBy = null,
    bool Desc = false, long? RoleId = null, bool? Blocked = null);
