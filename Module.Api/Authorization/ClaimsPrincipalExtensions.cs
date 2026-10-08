using System.Security.Claims;
using Module.Application.Common;

namespace Module.Api.Authorization;

public static class ClaimsPrincipalExtensions
{
    public static long? GetUserIdOrNull(this ClaimsPrincipal user) =>
        long.TryParse(user.FindFirst("sub")?.Value, out var id) ? id : null;

    public static long GetUserId(this ClaimsPrincipal user) =>
        user.GetUserIdOrNull() ?? throw AppException.Unauthorized("Token sin identificador de usuario.");
}
