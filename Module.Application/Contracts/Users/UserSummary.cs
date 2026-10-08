namespace Module.Application.Contracts.Users;

public record UserSummary(long Id, string Username, string Email, string Name, string Surname, string Role, bool Blocked);
