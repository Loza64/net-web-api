namespace Module.Application.Contracts.Roles;

public record SetRolePermissionsRequest(IReadOnlyList<long> PermissionIds);
