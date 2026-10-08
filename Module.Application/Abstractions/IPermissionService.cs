namespace Module.Application.Abstractions;

public interface IPermissionService
{
    Task<IReadOnlySet<string>> GetPermissionsAsync(long userId, CancellationToken ct = default);

    void InvalidateAll();
}
