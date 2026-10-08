namespace Module.Application.Contracts.Users;

public record UpdateUserRequest(string Username, string Email, string Name, string Surname, long RoleId, bool Blocked);
