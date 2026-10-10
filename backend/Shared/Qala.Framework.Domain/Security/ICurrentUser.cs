using System.Security.Claims;

namespace Qala.Framework.Domain.Security;

/// <summary>The signed-in user of the current request (or nobody).</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    /// <summary>The auth user id (the <c>sub</c> claim).</summary>
    Guid? Id { get; }

    string? UserName { get; }

    ClaimsPrincipal? Principal { get; }

    /// <summary>Whether the user's token carries <paramref name="permission"/>.</summary>
    bool HasPermission(string permission);
}

/// <summary>Claim types issued by Qala.Game.Auth.Host.</summary>
public static class QalaClaimTypes
{
    public const string Subject = "sub";
    public const string Name = "name";
    public const string Email = "email";
    public const string Role = "role";
    public const string Permission = "permission";
    public const string Locale = "locale";
}

public static class CurrentUserExtensions
{
    /// <summary>The user id, or throws <see cref="UnauthorizedAccessException"/> when nobody is signed in.</summary>
    public static Guid GetRequiredId(this ICurrentUser currentUser)
    {
        ArgumentNullException.ThrowIfNull(currentUser);
        return currentUser.Id ?? throw new UnauthorizedAccessException();
    }
}
