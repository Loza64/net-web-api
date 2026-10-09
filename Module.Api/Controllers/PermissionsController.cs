using Microsoft.AspNetCore.Mvc;
using Module.Api.Authorization;
using Module.Application.Contracts.Common;
using Module.Application.Contracts.Permissions;
using Module.Application.Services;
using Module.Domain.Constants;

namespace Module.Api.Controllers;

[ApiController]
[Route("api/permissions")]
public class PermissionsController(IPermissionCatalogService permissions) : ControllerBase
{
    [HttpGet, HasPermission(PermissionNames.PermissionsRead)]
    public Task<PagedResult<PermissionDto>> List([FromQuery] PermissionListQuery query, CancellationToken ct) =>
        permissions.ListAsync(query, ct);

    [HttpPut("{id:long}"), HasPermission(PermissionNames.RolesManage)]
    public Task<PermissionDto> Update(long id, PermissionUpdateRequest request, CancellationToken ct) =>
        permissions.UpdateAsync(id, request.Title, ct);
}
