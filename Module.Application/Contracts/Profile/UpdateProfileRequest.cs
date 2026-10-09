namespace Module.Application.Contracts.Profile;

public record UpdateProfileRequest(string Username, string Name, string Surname, string Email);
