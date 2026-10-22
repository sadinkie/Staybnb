namespace Staybnb.Data;

public static class RoleNames
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Host = "Host";
    public const string Guest = "Guest";

    /// <summary>For [Authorize(Roles = ...)] on pages both Admins and the SuperAdmin may open.</summary>
    public const string AdminOrSuperAdmin = "Admin,SuperAdmin";

    public static readonly string[] All = { SuperAdmin, Admin, Host, Guest };
}