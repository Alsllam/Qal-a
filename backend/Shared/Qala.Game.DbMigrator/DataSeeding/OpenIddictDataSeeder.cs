using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using Qala.Framework.Domain.DataSeeding;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Qala.Game.DbMigrator.DataSeeding;

/// <summary>
/// The API scopes (<c>players-api</c>, <c>matches-api</c>, <c>ai-api</c>; each scope's resource is its audience) and
/// the two public PKCE clients <c>qala-mobile</c> and <c>qala-admin</c>. Existing entries are updated in place.
/// </summary>
public sealed partial class OpenIddictDataSeeder(
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictScopeManager scopeManager,
    IOptions<SeedSettings> settings,
    ILogger<OpenIddictDataSeeder> logger) : IDataSeeder
{
    public const string MobileClientId = "qala-mobile";
    public const string AdminClientId = "qala-admin";

    public static readonly IReadOnlyList<string> ApiScopes = ["players-api", "matches-api", "ai-api"];

    public int Order => 2;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var scope in ApiScopes)
        {
            var descriptor = new OpenIddictScopeDescriptor { Name = scope, DisplayName = scope, Resources = { scope } };
            if (await scopeManager.FindByNameAsync(scope, cancellationToken) is { } existing)
            {
                await scopeManager.PopulateAsync(existing, descriptor, cancellationToken);
                await scopeManager.UpdateAsync(existing, cancellationToken);
            }
            else
            {
                await scopeManager.CreateAsync(descriptor, cancellationToken);
            }
        }

        await UpsertClientAsync(Client(MobileClientId, "Qal'a mobile app", settings.Value.MobileRedirectUris, []), cancellationToken);
        await UpsertClientAsync(Client(AdminClientId, "Qal'a admin console", settings.Value.AdminRedirectUris, settings.Value.AdminPostLogoutRedirectUris), cancellationToken);
        LogSeeded(logger, ApiScopes.Count);
    }

    /// <summary>A public client: authorization code + PKCE + refresh tokens, no secret, no consent screen.</summary>
    public static OpenIddictApplicationDescriptor Client(string clientId, string displayName, IEnumerable<string> redirectUris, IEnumerable<string> postLogoutRedirectUris)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            DisplayName = displayName,
            ClientType = ClientTypes.Public,
            ConsentType = ConsentTypes.Implicit,
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.Endpoints.EndSession,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Email,
                Permissions.Scopes.Profile,
                Permissions.Scopes.Roles,
            },
            Requirements = { Requirements.Features.ProofKeyForCodeExchange },
        };
        foreach (var scope in ApiScopes)
        {
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + scope);
        }

        foreach (var uri in redirectUris)
        {
            descriptor.RedirectUris.Add(new Uri(uri));
        }

        foreach (var uri in postLogoutRedirectUris)
        {
            descriptor.PostLogoutRedirectUris.Add(new Uri(uri));
        }

        return descriptor;
    }

    private async Task UpsertClientAsync(OpenIddictApplicationDescriptor descriptor, CancellationToken cancellationToken)
    {
        if (await applicationManager.FindByClientIdAsync(descriptor.ClientId!, cancellationToken) is { } existing)
        {
            await applicationManager.PopulateAsync(existing, descriptor, cancellationToken);
            await applicationManager.UpdateAsync(existing, cancellationToken);
        }
        else
        {
            await applicationManager.CreateAsync(descriptor, cancellationToken);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "OpenIddict: {ScopeCount} API scopes and the qala-mobile / qala-admin clients are up to date")]
    private static partial void LogSeeded(ILogger logger, int scopeCount);
}
