using Module.Application.Contracts.Common;

namespace Module.Application.Contracts.Roles;

public record CreateRoleRequest(
    string Name, bool Active = true, IReadOnlyList<IdRequest>? Permissions = null);
