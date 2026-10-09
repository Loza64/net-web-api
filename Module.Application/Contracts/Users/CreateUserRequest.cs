using Module.Application.Contracts.Common;

namespace Module.Application.Contracts.Users;

public record CreateUserRequest(
    string Username, string Email, string Password, string Name, string Surname, IdRequest Role);
