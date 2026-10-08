using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using Module.Application.Abstractions;
using Module.Domain.Constants;

namespace Module.Infrastructure.Auth;

public class PermissionService(IApplicationDbContext db, IMemoryCache cache) : IPermissionService
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(2);
    private static CancellationTokenSource _resetToken = new();

    public async Task<IReadOnlySet<string>> GetPermissionsAsync(long userId, CancellationToken ct = default)
    {
        var set = await cache.GetOrCreateAsync($"perms:{userId}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = Ttl;
            entry.AddExpirationToken(new CancellationChangeToken(Volatile.Read(ref _resetToken).Token));

            var roleName = await db.Users
                .AsNoTracking()
                .Where(u => u.Id == userId && !u.Blocked && u.Role.Active)
                .Select(u => u.Role.Name)
                .FirstOrDefaultAsync(ct);

            if (roleName is null) return new HashSet<string>();

            var names = roleName == RoleNames.SuperAdmin
                ? await db.Permissions.AsNoTracking().Select(p => p.Name).ToListAsync(ct)
                : await db.Users
                    .AsNoTracking()
                    .Where(u => u.Id == userId)
                    .SelectMany(u => u.Role.Permissions)
                    .Select(p => p.Name)
                    .ToListAsync(ct);

            return names.ToHashSet();
        });

        return set ?? new HashSet<string>();
    }

    public void InvalidateAll() =>
        Interlocked.Exchange(ref _resetToken, new CancellationTokenSource()).Cancel();
}
