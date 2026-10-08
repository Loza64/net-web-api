using Module.Domain.Common;

namespace Module.Domain.Entities;

public class User : BaseEntity
{
    public string Username { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Surname { get; set; } = default!;
    public string? Photo { get; set; }
    public bool Blocked { get; set; }

    public long RoleId { get; set; }
    public Role Role { get; set; } = default!;
    public List<RefreshToken> RefreshTokens { get; set; } = [];
}
