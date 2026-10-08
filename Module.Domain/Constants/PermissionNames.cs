namespace Module.Domain.Constants;

public static class PermissionNames
{
    public const string UsersRead = "users.read";
    public const string UsersManage = "users.manage";
    public const string RolesRead = "roles.read";
    public const string RolesManage = "roles.manage";
    public const string PermissionsRead = "permissions.read";

    public static readonly IReadOnlyList<(string Name, string Title)> All =
    [
        (UsersRead, "Ver usuarios"),
        (UsersManage, "Gestionar usuarios"),
        (RolesRead, "Ver roles"),
        (RolesManage, "Gestionar roles"),
        (PermissionsRead, "Ver permisos"),
    ];
}
