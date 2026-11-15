using System.Net;
using System.Net.Http.Json;
using Module.Application.Contracts.Common;
using Module.Application.Contracts.Permissions;
using Module.Application.Contracts.Roles;
using Module.Application.Contracts.Users;

namespace Module.Test.Integration;

[Trait("Category", "integration")]
public class ManagementApiTests : ModuleApiTestBase
{
    [Fact]
    [Trait("Category", "positive")]
    [Trait("Category", "critical-path")]
    [Trait("Category", "end-to-end")]
    public async Task Super_admin_can_manage_roles_users_and_permission_catalog()
    {
        // Arrange
        await LoginAsSuperAdminAsync();

        // Act
        var rolesResponse = await Client.GetAsync("/api/roles");

        // Assert
        Assert.True(rolesResponse.IsSuccessStatusCode,
            $"Roles request returned {(int)rolesResponse.StatusCode}: {rolesResponse.Headers.WwwAuthenticate}");
        var roles = await rolesResponse.Content.ReadFromJsonAsync<PagedResult<RoleDetail>>();
        Assert.NotNull(roles);
        Assert.Contains(roles.Data, role => role.Name == "ADMIN");

        var adminRole = roles.Data.Single(role => role.Name == "ADMIN");

        // Act
        var roleCreatedResponse = await Client.PostAsJsonAsync(
            "/api/roles", new CreateRoleRequest("Support", Permissions: []));
        var createdRole = await roleCreatedResponse.Content.ReadFromJsonAsync<RoleDetail>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, roleCreatedResponse.StatusCode);
        Assert.NotNull(createdRole);

        // Act
        var permissionPage = await Client.GetFromJsonAsync<PagedResult<PermissionDto>>("/api/permissions");

        // Assert
        Assert.NotNull(permissionPage);
        Assert.NotEmpty(permissionPage.Data);
        var permissionId = permissionPage.Data[0].Id;

        // Act
        var userCreatedResponse = await Client.PostAsJsonAsync(
            "/api/users",
            new CreateUserRequest(
                $"operator-{Guid.NewGuid():N}"[..18],
                $"{Guid.NewGuid():N}@example.com",
                "StrongPassword123",
                "Test",
                "Operator",
                new IdRequest(adminRole.Id)));
        var createdUser = await userCreatedResponse.Content.ReadFromJsonAsync<UserDetail>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, userCreatedResponse.StatusCode);
        Assert.NotNull(createdUser);

        // Act
        var updatedPermission = await Client.PutAsJsonAsync(
            $"/api/permissions/{permissionId}",
            new PermissionUpdateRequest(permissionPage.Data[0].Title));
        var rolePermissions = await Client.PutAsJsonAsync(
            $"/api/roles/{createdRole!.Id}/permissions", new SetRolePermissionsRequest([permissionId]));
        var roleUpdate = await Client.PutAsJsonAsync(
            $"/api/roles/{createdRole.Id}", new UpdateRoleRequest("SupportUpdated", true));

        // Assert
        Assert.Equal(HttpStatusCode.OK, updatedPermission.StatusCode);
        Assert.Equal(HttpStatusCode.OK, rolePermissions.StatusCode);
        Assert.Equal(HttpStatusCode.OK, roleUpdate.StatusCode);

        // Act
        var fetchedRole = await Client.GetAsync($"/api/roles/{createdRole.Id}");
        var fetchedUser = await Client.GetAsync($"/api/users/{createdUser!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, fetchedRole.StatusCode);
        Assert.Equal(HttpStatusCode.OK, fetchedUser.StatusCode);

        // Act
        var userUpdate = await Client.PutAsJsonAsync(
            $"/api/users/{createdUser.Id}",
            new UpdateUserRequest(
                createdUser.Username, "Updated", "Operator", false, createdUser.Email, new IdRequest(adminRole.Id)));
        var listedUsers = await Client.GetFromJsonAsync<PagedResult<UserDetail>>("/api/users?Search=updated");

        // Assert
        Assert.Equal(HttpStatusCode.OK, userUpdate.StatusCode);
        Assert.NotNull(listedUsers);
        Assert.Contains(listedUsers.Data, user => user.Id == createdUser.Id);

        // Act
        var userDelete = await Client.DeleteAsync($"/api/users/{createdUser.Id}");
        var userRestore = await Client.PatchAsync($"/api/users/{createdUser.Id}/restore", null);
        var roleDelete = await Client.DeleteAsync($"/api/roles/{createdRole.Id}");
        var roleRestore = await Client.PatchAsync($"/api/roles/{createdRole.Id}/restore", null);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, userDelete.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, userRestore.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, roleDelete.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, roleRestore.StatusCode);
    }
}
