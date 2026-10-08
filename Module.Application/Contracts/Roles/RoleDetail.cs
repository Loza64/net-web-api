using Module.Application.Contracts.Permissions;

namespace Module.Application.Contracts.Roles;

public record RoleDetail(
    long Id, string Name, bool Active, IReadOnlyList<PermissionDto> Permissions,
    DateTime CreatedAt, DateTime UpdatedAt, DateTime? DeletedAt);
