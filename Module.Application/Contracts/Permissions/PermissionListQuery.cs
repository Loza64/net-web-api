namespace Module.Application.Contracts.Permissions;

public record PermissionListQuery(int Page = 0, int Size = 30, string[]? Sort = null, string? Search = null);
