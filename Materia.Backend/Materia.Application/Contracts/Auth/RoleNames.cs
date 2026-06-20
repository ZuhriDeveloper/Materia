namespace Materia.Application.Contracts.Auth;

/// <summary>
/// Application role names. <see cref="AssignableStoreRoles"/> are the roles a platform
/// SuperAdmin may grant to store-scoped staff — deliberately excluding <see cref="SuperAdmin"/>
/// so the per-store user-management UI can never be used to escalate a user to platform admin.
/// </summary>
public static class RoleNames
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Cashier = "Cashier";
    public const string Gudang = "Gudang";
    public const string Helper = "Helper";

    public static readonly IReadOnlyList<string> AssignableStoreRoles = [Admin, Cashier, Gudang, Helper];
}
