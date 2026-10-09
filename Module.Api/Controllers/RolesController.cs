using Microsoft.AspNetCore.Mvc;
using Module.Application.Contracts.Common;
using Module.Application.Contracts.Roles;
using Module.Application.Services;
using Module.Api.Authorization;
using Module.Domain.Constants;

namespace Module.Api.Controllers;

[ApiController]
[Route("api/roles")]
public class RolesController(IRoleService roles) : ControllerBase
{
    [HttpGet, HasPermission(PermissionNames.RolesRead)]
    public Task<PagedResult<RoleDetail>> List([FromQuery] RoleListQuery query, CancellationToken ct) =>
        roles.ListAsync(query, ct);

    [HttpGet("{id:long}"), HasPermission(PermissionNames.RolesRead)]
    public Task<RoleDetail> Get(long id, CancellationToken ct) => roles.GetAsync(id, ct);

    [HttpPost, HasPermission(PermissionNames.RolesManage)]
    public async Task<ActionResult<RoleDetail>> Create(CreateRoleRequest request, CancellationToken ct)
    {
        var created = await roles.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}"), HasPermission(PermissionNames.RolesManage)]
    public Task<RoleDetail> Update(long id, UpdateRoleRequest request, CancellationToken ct) =>
        roles.UpdateAsync(id, request, ct);

    [HttpDelete("{id:long}"), HasPermission(PermissionNames.RolesManage)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await roles.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPatch("{id:long}/restore"), HasPermission(PermissionNames.RolesManage)]
    public async Task<IActionResult> Restore(long id, CancellationToken ct)
    {
        await roles.RestoreAsync(id, ct);
        return NoContent();
    }

    [HttpPut("{id:long}/permissions"), HasPermission(PermissionNames.RolesManage)]
    public Task<RoleDetail> SetPermissions(long id, SetRolePermissionsRequest request, CancellationToken ct) =>
        roles.SetPermissionsAsync(id, request, ct);
}
