namespace Module.Application.Contracts.Roles;

public record RoleSummary(long Id, string Name, bool Active, DateTime CreatedAt, DateTime UpdatedAt, DateTime? DeletedAt);
