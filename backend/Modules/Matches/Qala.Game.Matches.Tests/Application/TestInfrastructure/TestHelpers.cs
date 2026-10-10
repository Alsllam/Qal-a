using System.Security.Claims;
using Qala.Framework.Domain.Security;

namespace Qala.Game.Matches.Tests.Application.TestInfrastructure;

/// <summary>A clock the test moves by hand.</summary>
public sealed class ManualTimeProvider(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _now = start;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now += by;
}

/// <summary>A signed-in user whose id and permissions the test sets.</summary>
public sealed class TestCurrentUser : ICurrentUser
{
    public Guid? Id { get; set; }

    public string? UserName { get; set; } = "Tester";

    public HashSet<string> Permissions { get; } = [];

    public bool IsAuthenticated => Id is not null;

    public ClaimsPrincipal? Principal => null;

    public bool HasPermission(string permission) => Permissions.Contains(permission);

    /// <summary>Switches to <paramref name="userId"/> with no permissions.</summary>
    public void SignIn(Guid userId, string name = "Tester", params string[] permissions)
    {
        Id = userId;
        UserName = name;
        Permissions.Clear();
        Permissions.UnionWith(permissions);
    }
}
