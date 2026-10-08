using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Module.Application.Abstractions;

namespace Module.Api.Authorization;

public sealed class HasPermissionAttribute(string permission)
    : AuthorizeAttribute($"{PolicyPrefix}{permission}")
{
    public const string PolicyPrefix = "perm:";
}

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;

public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(HasPermissionAttribute.PolicyPrefix, StringComparison.Ordinal))
        {
            return new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(policyName[HasPermissionAttribute.PolicyPrefix.Length..]))
                .Build();
        }
        return await base.GetPolicyAsync(policyName);
    }
}

public sealed class PermissionHandler(IPermissionService permissions) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var userId = context.User.GetUserIdOrNull();
        if (userId is null) return;

        var granted = await permissions.GetPermissionsAsync(userId.Value);
        if (granted.Contains(requirement.Permission))
            context.Succeed(requirement);
    }
}

public static class PermissionAuthorizationExtensions
{
    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionHandler>();
        return services;
    }
}
