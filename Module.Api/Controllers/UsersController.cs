using Microsoft.AspNetCore.Mvc;
using Module.Api.Authorization;
using Module.Application.Contracts.Common;
using Module.Application.Contracts.Profile;
using Module.Application.Contracts.Users;
using Module.Application.Services;
using Module.Domain.Constants;

namespace Module.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(IUserService users) : ControllerBase
{
    [HttpGet, HasPermission(PermissionNames.UsersRead)]
    public Task<PagedResult<UserDetail>> List([FromQuery] UserListQuery query, CancellationToken ct) =>
        users.ListAsync(query, ct);

    [HttpGet("{id:long}"), HasPermission(PermissionNames.UsersRead)]
    public Task<UserDetail> Get(long id, CancellationToken ct) => users.GetAsync(id, ct);

    [HttpPost, HasPermission(PermissionNames.UsersManage)]
    public async Task<ActionResult<UserDetail>> Create(CreateUserRequest request, CancellationToken ct)
    {
        var created = await users.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}"), HasPermission(PermissionNames.UsersManage)]
    public Task<UserDetail> Update(long id, UpdateUserRequest request, CancellationToken ct) =>
        users.UpdateAsync(id, request, User.GetUserId(), ct);

    [HttpDelete("{id:long}"), HasPermission(PermissionNames.UsersManage)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await users.DeleteAsync(id, User.GetUserId(), ct);
        return NoContent();
    }

    [HttpPatch("{id:long}/restore"), HasPermission(PermissionNames.UsersManage)]
    public async Task<IActionResult> Restore(long id, CancellationToken ct)
    {
        await users.RestoreAsync(id, ct);
        return NoContent();
    }
}
