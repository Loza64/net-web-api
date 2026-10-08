using Microsoft.EntityFrameworkCore;
using Module.Application.Abstractions;
using Module.Application.Common;
using Module.Application.Contracts.Auth;
using Module.Application.Mappings;
using Module.Domain.Constants;
using Module.Domain.Entities;

namespace Module.Application.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct);
    Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct);
    Task LogoutAsync(LogoutRequest request, CancellationToken ct);
}

public class AuthService(IApplicationDbContext db, IPasswordHasher hasher, ITokenService tokens) : IAuthService
{
    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users
            .Include(u => u.Role).ThenInclude(r => r.Permissions)
            .FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null || !hasher.Verify(request.Password, user.Password))
            throw AppException.Unauthorized();

        EnsureCanAuthenticate(user);
        return await IssueAsync(user, Guid.NewGuid(), ct);
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        var username = request.Username.Trim().ToLowerInvariant();
        var email = request.Email.Trim().ToLowerInvariant();

        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            throw AppException.Conflict("El email ya está en uso.");
        if (await db.Users.AnyAsync(u => u.Username == username, ct))
            throw AppException.Conflict("El nombre de usuario ya está en uso.");

        var role = await db.Roles.Include(r => r.Permissions)
                       .FirstOrDefaultAsync(r => r.Name == RoleNames.Admin && r.Active, ct)
                   ?? throw AppException.Conflict("El registro no está disponible: falta el rol por defecto.");

        var user = new User
        {
            Username = username,
            Email = email,
            Password = hasher.Hash(request.Password),
            Name = request.Name.Trim(),
            Surname = request.Surname.Trim(),
            RoleId = role.Id,
            Role = role
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return await IssueAsync(user, Guid.NewGuid(), ct);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct)
    {
        var hash = tokens.HashRefreshToken(request.RefreshToken);
        var stored = await db.RefreshTokens
            .Include(t => t.User).ThenInclude(u => u.Role).ThenInclude(r => r.Permissions)
            .FirstOrDefaultAsync(t => t.Token == hash, ct);

        if (stored?.User is null)
            throw AppException.Unauthorized("Refresh token inválido.");

        if (stored.Used || stored.Revoked)
        {
            await RevokeFamilyAsync(stored.FamilyId, ct);
            throw AppException.Unauthorized("Refresh token inválido.");
        }

        if (stored.ExpiresAt <= DateTime.UtcNow)
            throw AppException.Unauthorized("Refresh token expirado.");

        EnsureCanAuthenticate(stored.User);

        var marked = await db.RefreshTokens
            .Where(t => t.Id == stored.Id && !t.Used)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Used, true), ct);

        if (marked == 0)
        {
            await RevokeFamilyAsync(stored.FamilyId, ct);
            throw AppException.Unauthorized("Refresh token inválido.");
        }

        return await IssueAsync(stored.User, stored.FamilyId, ct);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken ct)
    {
        var hash = tokens.HashRefreshToken(request.RefreshToken);
        var familyId = await db.RefreshTokens
            .Where(t => t.Token == hash)
            .Select(t => (Guid?)t.FamilyId)
            .FirstOrDefaultAsync(ct);

        if (familyId is not null)
            await RevokeFamilyAsync(familyId.Value, ct);
    }

    private static void EnsureCanAuthenticate(User user)
    {
        if (user.Blocked) throw AppException.Forbidden("Usuario bloqueado.");
        if (!user.Role.Active) throw AppException.Forbidden("El rol del usuario está inactivo.");
    }

    private Task<int> RevokeFamilyAsync(Guid familyId, CancellationToken ct) =>
        db.RefreshTokens
            .Where(t => t.FamilyId == familyId && !t.Revoked)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Revoked, true), ct);

    private async Task<AuthResponse> IssueAsync(User user, Guid familyId, CancellationToken ct)
    {
        var refresh = tokens.CreateRefreshToken();
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refresh.Hash,
            FamilyId = familyId,
            ExpiresAt = refresh.ExpiresAt
        });
        await db.SaveChangesAsync(ct);

        return new AuthResponse(tokens.CreateAccessToken(user), refresh.Raw, user.ToDetail());
    }
}
