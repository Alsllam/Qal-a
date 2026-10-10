using System.Collections.Immutable;
using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Qala.Framework.Domain.Identity;
using Qala.Framework.Domain.Security;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Qala.Game.Auth.Host.Controllers;

/// <summary>
/// OpenIddict pass-through endpoints. Clients are first-party (mobile app, admin console), so there is no consent
/// screen: a signed-in user is sent straight back with a code.
/// </summary>
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class AuthorizationController(
    UserManager<AppUser> userManager,
    RoleManager<AppRole> roleManager,
    SignInManager<AppUser> signInManager,
    IOpenIddictScopeManager scopeManager) : Controller
{
    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        if (!result.Succeeded || request.HasPromptValue(PromptValues.Login))
        {
            if (request.HasPromptValue(PromptValues.None))
            {
                return Forbid(Error(Errors.LoginRequired, "The user is not signed in."), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var parameters = Request.HasFormContentType ? Request.Form.ToList() : Request.Query.ToList();
            parameters.RemoveAll(p => p.Key == Parameters.Prompt);
            return Challenge(
                new AuthenticationProperties { RedirectUri = Request.PathBase + Request.Path + QueryString.Create(parameters) },
                IdentityConstants.ApplicationScheme);
        }

        var user = await userManager.GetUserAsync(result.Principal);
        if (user is null || !await signInManager.CanSignInAsync(user) || await userManager.IsLockedOutAsync(user))
        {
            return Forbid(Error(Errors.AccessDenied, "The account cannot sign in."), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var principal = await CreatePrincipalAsync(user, request.GetScopes());
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("~/connect/token")]
    [IgnoreAntiforgeryToken]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");
        if (!request.IsAuthorizationCodeGrantType() && !request.IsRefreshTokenGrantType())
        {
            return Forbid(Error(Errors.UnsupportedGrantType, "Only the authorization code and refresh token grants are supported."), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        var user = result.Principal?.GetClaim(Claims.Subject) is { } subject ? await userManager.FindByIdAsync(subject) : null;
        if (user is null || !await signInManager.CanSignInAsync(user) || await userManager.IsLockedOutAsync(user))
        {
            return Forbid(Error(Errors.InvalidGrant, "The token is no longer valid."), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        // Rebuild the identity so role and permission changes apply at the next refresh.
        var principal = await CreatePrincipalAsync(user, result.Principal!.GetScopes());
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpGet("~/connect/userinfo")]
    [HttpPost("~/connect/userinfo")]
    [Authorize(AuthenticationSchemes = OpenIddict.Validation.AspNetCore.OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<IActionResult> UserInfo()
    {
        var user = await userManager.FindByIdAsync(User.GetClaim(Claims.Subject) ?? string.Empty);
        if (user is null)
        {
            return Challenge(OpenIddict.Validation.AspNetCore.OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        }

        var claims = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            [Claims.Subject] = user.Id.ToString(),
            [Claims.Name] = user.DisplayName ?? user.UserName,
        };
        if (User.HasScope(Scopes.Email))
        {
            claims[Claims.Email] = user.Email;
            claims[Claims.EmailVerified] = user.EmailConfirmed;
        }

        return Ok(claims);
    }

    [HttpGet("~/connect/logout")]
    [HttpPost("~/connect/logout")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return SignOut(new AuthenticationProperties { RedirectUri = "/" }, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private async Task<ClaimsPrincipal> CreatePrincipalAsync(AppUser user, ImmutableArray<string> scopes)
    {
        var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType, Claims.Name, Claims.Role);
        identity.SetClaim(Claims.Subject, user.Id.ToString())
            .SetClaim(Claims.Name, user.DisplayName ?? user.UserName)
            .SetClaim(Claims.Email, user.Email);

        var roles = await userManager.GetRolesAsync(user);
        identity.SetClaims(Claims.Role, [.. roles]);
        var permissions = new HashSet<string>(StringComparer.Ordinal);
        foreach (var roleName in roles)
        {
            if (await roleManager.FindByNameAsync(roleName) is { } role)
            {
                permissions.UnionWith((await roleManager.GetClaimsAsync(role)).Where(c => c.Type == QalaClaimTypes.Permission).Select(c => c.Value));
            }
        }

        identity.SetClaims(QalaClaimTypes.Permission, [.. permissions]);
        identity.SetScopes(scopes);
        var resources = new List<string>();
        await foreach (var resource in scopeManager.ListResourcesAsync(identity.GetScopes()))
        {
            resources.Add(resource);
        }

        identity.SetResources(resources);
        identity.SetDestinations(claim => GetDestinations(claim, identity));
        return new ClaimsPrincipal(identity);
    }

    private static IEnumerable<string> GetDestinations(Claim claim, ClaimsIdentity identity) => claim.Type switch
    {
        Claims.Subject => [Destinations.AccessToken, Destinations.IdentityToken],
        Claims.Name when identity.HasScope(Scopes.Profile) => [Destinations.AccessToken, Destinations.IdentityToken],
        Claims.Name => [Destinations.AccessToken],
        Claims.Email when identity.HasScope(Scopes.Email) => [Destinations.AccessToken, Destinations.IdentityToken],
        Claims.Role when identity.HasScope(Scopes.Roles) => [Destinations.AccessToken, Destinations.IdentityToken],
        Claims.Role => [Destinations.AccessToken],
        QalaClaimTypes.Permission => [Destinations.AccessToken],
        // Never copy the security stamp or other internal claims into tokens.
        _ => [],
    };

    private static AuthenticationProperties Error(string error, string description) => new(new Dictionary<string, string?>
    {
        [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description,
    });
}
