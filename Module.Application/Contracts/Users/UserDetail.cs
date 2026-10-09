using Module.Application.Contracts.Roles;

namespace Module.Application.Contracts.Users;

public record UserDetail(
    long Id, string Username, string Name, string Surname, string Email, bool Blocked,
    RoleDetail Role, DateTime CreatedAt, DateTime UpdatedAt, DateTime? DeletedAt);
