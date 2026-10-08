namespace Module.Application.Contracts.Profile;

public record UpdateProfileRequest(string Username, string Email, string Name, string Surname, string? Photo = null);
