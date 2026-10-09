namespace Module.Application.Contracts.Users;

public record UserListQuery(int Page = 0, int Size = 30, string? Search = null, long? Role = null,
    bool Deleted = false, string[]? Sort = null, bool? Blocked = null);
