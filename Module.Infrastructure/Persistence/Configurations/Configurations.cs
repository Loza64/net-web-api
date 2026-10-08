using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Domain.Entities;

namespace Module.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("USERS");
        b.Property(x => x.Username).HasMaxLength(50).IsRequired();
        b.Property(x => x.Email).HasMaxLength(255).IsRequired();
        b.Property(x => x.Password).HasMaxLength(100).IsRequired();
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Surname).HasMaxLength(100).IsRequired();
        b.Property(x => x.Photo).HasMaxLength(500);

        b.HasIndex(x => new { x.Email, x.DeletedAt }).IsUnique();
        b.HasIndex(x => new { x.Username, x.DeletedAt }).IsUnique();

        b.HasOne(x => x.Role).WithMany(r => r.Users)
            .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);

        b.HasQueryFilter(x => x.DeletedAt == null);
    }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("ROLES");
        b.Property(x => x.Name).HasMaxLength(50).IsRequired();
        b.HasIndex(x => new { x.Name, x.DeletedAt }).IsUnique();
        b.HasQueryFilter(x => x.DeletedAt == null);

        b.HasMany(r => r.Permissions).WithMany(p => p.Roles)
            .UsingEntity<Dictionary<string, object>>(
                "ROLE_PERMISSIONS",
                j => j.HasOne<Permission>().WithMany().HasForeignKey("PERMISSION_ID"),
                j => j.HasOne<Role>().WithMany().HasForeignKey("ROLE_ID"),
                j =>
                {
                    j.ToTable("ROLE_PERMISSIONS");
                    j.HasKey("ROLE_ID", "PERMISSION_ID");
                });
    }
}

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> b)
    {
        b.ToTable("PERMISSIONS");
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.Title).HasMaxLength(150).IsRequired();
        b.HasIndex(x => x.Name).IsUnique();
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("REFRESH_TOKEN");
        b.Property(x => x.Token).HasMaxLength(128).IsRequired();
        b.HasIndex(x => x.Token).IsUnique();
        b.HasIndex(x => x.FamilyId);
        b.HasOne(x => x.User).WithMany(u => u.RefreshTokens)
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
