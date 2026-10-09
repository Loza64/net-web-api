using Module.Application.Contracts.Permissions;
using Module.Application.Contracts.Profile;
using Module.Application.Contracts.Roles;
using Module.Application.Contracts.Users;
using Module.Domain.Entities;

namespace Module.Application.Mappings;

public static class EntityMappings
{
    public static PermissionDto ToDto(this Permission p) => new(p.Id, p.Name, p.Title);

    public static RoleDetail ToDetail(this Role r) =>
        new(r.Id, r.Name, r.Active,
            r.Permissions.OrderBy(p => p.Name).Select(p => p.ToDto()).ToList(),
            r.CreatedAt, r.UpdatedAt, r.DeletedAt);

    public static UserDetail ToDetail(this User u) =>
        new(u.Id, u.Username, u.Name, u.Surname, u.Email, u.Blocked, u.Role.ToDetail(),
            u.CreatedAt, u.UpdatedAt, u.DeletedAt);

    public static ProfileResponse ToProfileResponse(this User u) =>
        new(u.Username, u.Name, u.Surname, u.Email, u.Blocked, u.Role.ToDetail(),
            u.CreatedAt, u.UpdatedAt, u.DeletedAt);
}
