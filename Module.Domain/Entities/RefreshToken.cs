namespace Module.Domain.Entities;

public class RefreshToken
{
    public long Id { get; set; }
    public string Token { get; set; } = default!;
    public Guid FamilyId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool Used { get; set; }
    public bool Revoked { get; set; }

    public long UserId { get; set; }
    public User User { get; set; } = default!;
}
