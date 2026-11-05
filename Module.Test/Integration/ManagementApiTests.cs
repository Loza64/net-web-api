using System.Net;
using System.Net.Http.Json;
using Module.Application.Contracts.Common;
using Module.Application.Contracts.Permissions;
using Module.Application.Contracts.Roles;
using Module.Application.Contracts.Users;

namespace Module.Test.Integration;

public class ManagementApiTests : ModuleApiTestBase
{
    [Fact]
    public async Task Super_admin_can_manage_roles_users_and_permission_catalog()
    {
        await LoginAsSuperAdminAsync();

        var rolesResponse = await Client.GetAsync("/api/roles");
        Assert.True(rolesResponse.IsSuccessStatusCode,
            $"Roles request returned {(int)rolesResponse.StatusCode}: {rolesResponse.Headers.WwwAuthenticate}");
        var roles = await rolesResponse.Content.ReadFromJsonAsync<PagedResult<RoleDetail>>();
        Assert.NotNull(roles);
        Assert.Contains(roles.Data, role => role.Name == "ADMIN");

        var adminRole = roles.Data.Single(role => role.Name == "ADMIN");
        var roleCreatedResponse = await Client.PostAsJsonAsync(
            "/api/roles", new CreateRoleRequest("Support", Permissions: []));
        Assert.Equal(HttpStatusCode.Created, roleCreatedResponse.StatusCode);
        var createdRole = await roleCreatedResponse.Content.ReadFromJsonAsync<RoleDetail>();
        Assert.NotNull(createdRole);

        var permissionPage = await Client.GetFromJsonAsync<PagedResult<PermissionDto>>("/api/permissions");
        Assert.NotNull(permissionPage);
        Assert.NotEmpty(permissionPage.Data);
        var permissionId = permissionPage.Data[0].Id;

        var userCreatedResponse = await Client.PostAsJsonAsync(
            "/api/users",
            new CreateUserRequest(
                $"operator-{Guid.NewGuid():N}"[..18],
                $"{Guid.NewGuid():N}@example.com",
                "StrongPassword123",
                "Test",
                "Operator",
                new IdRequest(adminRole.Id)));
        Assert.Equal(HttpStatusCode.Created, userCreatedResponse.StatusCode);
        var createdUser = await userCreatedResponse.Content.ReadFromJsonAsync<UserDetail>();
        Assert.NotNull(createdUser);

        var updatedPermission = await Client.PutAsJsonAsync(
            $"/api/permissions/{permissionId}",
            new PermissionUpdateRequest(permissionPage.Data[0].Title));
        Assert.Equal(HttpStatusCode.OK, updatedPermission.StatusCode);

        var rolePermissions = await Client.PutAsJsonAsync(
            $"/api/roles/{createdRole.Id}/permissions", new SetRolePermissionsRequest([permissionId]));
        Assert.Equal(HttpStatusCode.OK, rolePermissions.StatusCode);
        var roleUpdate = await Client.PutAsJsonAsync(
            $"/api/roles/{createdRole.Id}", new UpdateRoleRequest("SupportUpdated", true));
        Assert.Equal(HttpStatusCode.OK, roleUpdate.StatusCode);

        var fetchedRole = await Client.GetAsync($"/api/roles/{createdRole.Id}");
        Assert.Equal(HttpStatusCode.OK, fetchedRole.StatusCode);
        var fetchedUser = await Client.GetAsync($"/api/users/{createdUser.Id}");
        Assert.Equal(HttpStatusCode.OK, fetchedUser.StatusCode);

        var userUpdate = await Client.PutAsJsonAsync(
            $"/api/users/{createdUser.Id}",
            new UpdateUserRequest(
                createdUser.Username, "Updated", "Operator", false, createdUser.Email, new IdRequest(adminRole.Id)));
        Assert.Equal(HttpStatusCode.OK, userUpdate.StatusCode);
        var listedUsers = await Client.GetFromJsonAsync<PagedResult<UserDetail>>("/api/users?Search=updated");
        Assert.NotNull(listedUsers);
        Assert.Contains(listedUsers.Data, user => user.Id == createdUser.Id);

        var userDelete = await Client.DeleteAsync($"/api/users/{createdUser.Id}");
        Assert.Equal(HttpStatusCode.NoContent, userDelete.StatusCode);
        var userRestore = await Client.PatchAsync($"/api/users/{createdUser.Id}/restore", null);
        Assert.Equal(HttpStatusCode.NoContent, userRestore.StatusCode);

        var roleDelete = await Client.DeleteAsync($"/api/roles/{createdRole.Id}");
        Assert.Equal(HttpStatusCode.NoContent, roleDelete.StatusCode);
        var roleRestore = await Client.PatchAsync($"/api/roles/{createdRole.Id}/restore", null);
        Assert.Equal(HttpStatusCode.NoContent, roleRestore.StatusCode);
    }
}
