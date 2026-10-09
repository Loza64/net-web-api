using Module.Application.Contracts.Common;

namespace Module.Application.Contracts.Roles;

public record UpdateRoleRequest(
    string Name, bool Active, IReadOnlyList<IdRequest>? Permissions = null);
