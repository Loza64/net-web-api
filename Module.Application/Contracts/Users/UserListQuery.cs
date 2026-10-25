namespace Module.Application.Contracts.Users;

public record UserListQuery(int Page = 1, int PageSize = 30, string? Search = null, long? Role = null,
    bool Deleted = false, string[]? Sort = null, bool? Blocked = null);
