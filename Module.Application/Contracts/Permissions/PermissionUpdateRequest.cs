using System.ComponentModel.DataAnnotations;

namespace Module.Application.Contracts.Permissions;

public sealed record PermissionUpdateRequest(
    [property: Required, StringLength(100, MinimumLength = 3)] string Title);
