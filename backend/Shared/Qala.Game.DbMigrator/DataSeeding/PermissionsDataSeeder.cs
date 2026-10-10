using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qala.Framework.Domain.DataSeeding;
using Qala.Framework.Domain.Identity;
using Qala.Framework.Domain.Security;

namespace Qala.Game.DbMigrator.DataSeeding;

/// <summary><c>Seed</c> section. The admin password only ever comes from the environment or user-secrets.</summary>
public sealed class SeedSettings
{
    public const string SectionName = "Seed";

    public string? AdminEmail { get; set; }

    public string? AdminPassword { get; set; }

    public string[] MobileRedirectUris { get; set; } = ["qala://auth/callback"];

    public string[] AdminRedirectUris { get; set; } = ["http://localhost:4200/auth/callback"];

    public string[] AdminPostLogoutRedirectUris { get; set; } = ["http://localhost:4200/"];
}

/// <summary>
/// Gives the <c>admin</c> role every permission of every module (as role claims) and, when
/// <c>Seed__AdminEmail</c> / <c>Seed__AdminPassword</c> are set, creates the first admin account. Idempotent.
/// </summary>
public sealed partial class PermissionsDataSeeder(
    RoleManager<AppRole> roleManager,
    UserManager<AppUser> userManager,
    IEnumerable<IPermissionDefinitionProvider> providers,
    IOptions<SeedSettings> settings,
    TimeProvider timeProvider,
    ILogger<PermissionsDataSeeder> logger) : IDataSeeder
{
    public int Order => 1;

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var role = await roleManager.FindByNameAsync(AppRole.Admin);
        if (role is null)
        {
            role = new AppRole(AppRole.Admin);
            EnsureSucceeded(await roleManager.CreateAsync(role));
        }

        var existing = (await roleManager.GetClaimsAsync(role))
            .Where(c => c.Type == QalaClaimTypes.Permission)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.Ordinal);
        var all = providers.SelectMany(p => p.GetPermissions()).Distinct(StringComparer.Ordinal).ToList();
        foreach (var permission in all.Where(p => !existing.Contains(p)))
        {
            EnsureSucceeded(await roleManager.AddClaimAsync(role, new Claim(QalaClaimTypes.Permission, permission)));
        }

        LogPermissions(logger, all.Count, all.Count(p => !existing.Contains(p)));
        await SeedAdminAsync();
    }

    private async Task SeedAdminAsync()
    {
        var email = settings.Value.AdminEmail;
        var password = settings.Value.AdminPassword;
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            LogNoAdmin(logger);
            return;
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = "Admin",
                CreationTime = timeProvider.GetUtcNow().UtcDateTime,
            };
            EnsureSucceeded(await userManager.CreateAsync(user, password));
        }

        if (!await userManager.IsInRoleAsync(user, AppRole.Admin))
        {
            EnsureSucceeded(await userManager.AddToRoleAsync(user, AppRole.Admin));
        }
    }

    private static void EnsureSucceeded(IdentityResult result)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Admin role has {Total} permissions ({Added} added)")]
    private static partial void LogPermissions(ILogger logger, int total, int added);

    [LoggerMessage(Level = LogLevel.Information, Message = "Seed:AdminEmail / Seed:AdminPassword not set; no admin account created")]
    private static partial void LogNoAdmin(ILogger logger);
}
