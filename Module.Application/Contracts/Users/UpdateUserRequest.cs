using Module.Application.Contracts.Common;

namespace Module.Application.Contracts.Users;

public record UpdateUserRequest(
    string Username, string Name, string Surname, bool Blocked, string Email, IdRequest Role);
