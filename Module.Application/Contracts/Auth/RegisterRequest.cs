namespace Module.Application.Contracts.Auth;

public record RegisterRequest(string Username, string Email, string Password, string Name, string Surname);
