using Microsoft.AspNetCore.Identity;

namespace Qala.Framework.Domain.Identity;

/// <summary>An account (ASP.NET Core Identity, Guid keys). Owned by Qala.Game.Auth.Host.</summary>
public class AppUser : IdentityUser<Guid>
{
    public string? DisplayName { get; set; }

    public DateTime CreationTime { get; set; }
}

/// <summary>A role. Permissions are stored as role claims of type <c>permission</c>.</summary>
public class AppRole : IdentityRole<Guid>
{
    public const string Admin = "admin";

    public AppRole()
    {
    }

    public AppRole(string roleName)
        : base(roleName)
    {
    }
}
