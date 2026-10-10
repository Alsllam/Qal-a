using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Qala.Framework.Application.Extensions;
using Qala.Framework.Application.Options;
using Qala.Framework.Domain.Identity;
using Qala.Framework.Domain.Security;
using Qala.Framework.EntityFrameworkCore.Identity;
using Qala.Game.Auth.Host;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;
var settings = configuration.GetSection(AuthServerSettings.SectionName).Get<AuthServerSettings>() ?? new AuthServerSettings();

var connectionString = configuration.GetConnectionString(QalaIdentityDbContext.ConnectionStringName)
    ?? throw new InvalidOperationException("Connection string 'Auth' is missing (set ConnectionStrings__Auth).");
services.AddDbContext<QalaIdentityDbContext>(options =>
{
    options.UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", QalaIdentityDbContext.Schema));
    options.UseOpenIddict<Guid>();
});

services.AddIdentity<AppUser, AppRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.ClaimsIdentity.UserIdClaimType = Claims.Subject;
        options.ClaimsIdentity.UserNameClaimType = Claims.Name;
        options.ClaimsIdentity.RoleClaimType = Claims.Role;
    })
    .AddEntityFrameworkStores<QalaIdentityDbContext>()
    .AddDefaultTokenProviders();
services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
    options.Cookie.Name = ".Qala.Auth";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

services.AddOpenIddict()
    .AddCore(options => options.UseEntityFrameworkCore().UseDbContext<QalaIdentityDbContext>().ReplaceDefaultEntities<Guid>())
    .AddServer(options =>
    {
        options.SetIssuer(new Uri(settings.Issuer));
        options.SetAuthorizationEndpointUris("connect/authorize")
            .SetTokenEndpointUris("connect/token")
            .SetEndSessionEndpointUris("connect/logout")
            .SetUserInfoEndpointUris("connect/userinfo");
        options.AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange();
        options.AllowRefreshTokenFlow();
        options.RegisterScopes([Scopes.OpenId, Scopes.Profile, Scopes.Email, Scopes.Roles, Scopes.OfflineAccess, .. ApiScopes.All]);
        options.SetAccessTokenLifetime(TimeSpan.FromMinutes(settings.AccessTokenLifetimeMinutes));
        options.SetRefreshTokenLifetime(TimeSpan.FromDays(settings.RefreshTokenLifetimeDays));

        // The APIs validate tokens locally from the published signing keys, so access tokens are signed, not encrypted.
        options.DisableAccessTokenEncryption();
        if (builder.Environment.IsDevelopment())
        {
            options.AddDevelopmentEncryptionCertificate().AddDevelopmentSigningCertificate();
        }
        else
        {
            options.AddSigningCertificate(LoadCertificate(settings.SigningCertificatePath, settings.SigningCertificatePassword, "signing"));
            options.AddEncryptionCertificate(LoadCertificate(settings.EncryptionCertificatePath, settings.EncryptionCertificatePassword, "encryption"));
        }

        var aspNetCore = options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableTokenEndpointPassthrough()
            .EnableEndSessionEndpointPassthrough()
            .EnableUserInfoEndpointPassthrough();
        if (builder.Environment.IsDevelopment())
        {
            aspNetCore.DisableTransportSecurityRequirement();
        }
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

services.AddControllersWithViews();
services.AddApiDefinition();
services.AddLocalizationService(typeof(AuthServerSettings).Assembly);
services.AddCORSExtensions(configuration);
services.AddHttpContextAccessor();
services.AddLoggingService(configuration);
services.AddPermissionAuthorization();
services.AddSlidingWindowRateLimiterStrategy(configuration);
services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy());

var app = builder.Build();

app.UseLocalizationMiddleware();
app.UseLoggingMiddleware();
app.UseExceptionHandlingMiddleware();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(CorsSettings.PolicyName);
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers().RequireRateLimiting(RateLimiterSettings.PolicyName);

await app.RunAsync();

static X509Certificate2 LoadCertificate(string? path, string? password, string purpose)
{
    if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
    {
        throw new InvalidOperationException($"The {purpose} certificate is missing: set AuthServer__{char.ToUpperInvariant(purpose[0])}{purpose[1..]}CertificatePath (and its password) outside Development.");
    }

    return X509CertificateLoader.LoadPkcs12FromFile(path, password);
}

/// <summary>Entry point; public so integration tests can host it.</summary>
public partial class Program;
