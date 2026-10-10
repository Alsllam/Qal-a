using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Qala.Framework.Domain.Security;

namespace Qala.Framework.Application.Security;

/// <summary><see cref="ICurrentUser"/> read from the current HTTP request's claims.</summary>
public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? Id => GetUserId(Principal);

    public string? UserName => Principal?.FindFirst(QalaClaimTypes.Name)?.Value ?? Principal?.Identity?.Name;

    public bool HasPermission(string permission) => Principal?.HasClaim(QalaClaimTypes.Permission, permission) == true;

    /// <summary>Reads the <c>sub</c> claim (or NameIdentifier) as a Guid.</summary>
    public static Guid? GetUserId(ClaimsPrincipal? principal)
    {
        var value = principal?.FindFirst(QalaClaimTypes.Subject)?.Value ?? principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
