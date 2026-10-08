namespace Module.Application.Contracts.Roles;

public record CreateRoleRequest(string Name, bool Active = true);
