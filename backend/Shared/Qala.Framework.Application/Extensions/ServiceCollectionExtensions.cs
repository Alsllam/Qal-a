using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.OpenApi.Models;
using OpenIddict.Validation.AspNetCore;
using Qala.Framework.Application.Authorization;
using Qala.Framework.Application.DynamicControllers;
using Qala.Framework.Application.ExceptionHandling;
using Qala.Framework.Application.Localization;
using Qala.Framework.Application.Options;
using Qala.Framework.Application.Security;
using Qala.Framework.Domain.Security;
using Serilog;

namespace Qala.Framework.Application.Extensions;

/// <summary>Host composition helpers, called from each host's <c>Program.cs</c> in the documented order.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>CORS with an explicit origin list from <c>Cors:AllowedOrigins</c>.</summary>
    public static IServiceCollection AddCORSExtensions(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>() ?? new CorsSettings();
        services.AddCors(options => options.AddPolicy(CorsSettings.PolicyName, policy =>
        {
            if (settings.AllowedOrigins.Length > 0)
            {
                policy.WithOrigins(settings.AllowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
            }
        }));
        return services;
    }

    /// <summary>API explorer, the ModelState → exception filter and <see cref="ICurrentUser"/>.</summary>
    public static IServiceCollection AddApiDefinition(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.Configure<MvcOptions>(options => options.Filters.Add<ModelStateValidationFilter>());
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<ICurrentUser, CurrentUser>();
        return services;
    }

    /// <summary>JSON localization from <c>Resources/{ar,en}.json</c> embedded in the framework and the given module assemblies.</summary>
    public static IServiceCollection AddLocalizationService(this IServiceCollection services, params Assembly[] resourceAssemblies)
    {
        var assemblies = resourceAssemblies.Prepend(typeof(ServiceCollectionExtensions).Assembly).ToList();
        services.AddSingleton(new JsonLocalizationStore(assemblies));
        services.AddSingleton<IStringLocalizerFactory, JsonStringLocalizerFactory>();
        services.AddSingleton<IStringLocalizer, JsonStringLocalizer>();
        services.TryAddTransient(typeof(IStringLocalizer<>), typeof(StringLocalizer<>));
        return services;
    }

    /// <summary>Swagger with a bearer token button. The UI is mapped only in Development.</summary>
    public static IServiceCollection AddSwaggerService(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(SwaggerSettings.SectionName).Get<SwaggerSettings>() ?? new SwaggerSettings();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(settings.Version, new OpenApiInfo { Title = settings.Title, Version = settings.Version });
            options.CustomSchemaIds(type => type.FullName?.Replace('+', '.'));
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
            });
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = [],
            });
            foreach (var xml in Directory.EnumerateFiles(AppContext.BaseDirectory, "Qala.*.xml"))
            {
                options.IncludeXmlComments(xml);
            }
        });
        return services;
    }

    /// <summary>Serilog: console + rolling file (7-day retention), overridable from the <c>Serilog</c> section.</summary>
    public static IServiceCollection AddLoggingService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSerilog((_, logger) => logger
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(Path.Combine("Logs", "log-.txt"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7));
        return services;
    }

    /// <summary>
    /// OpenIddict token validation against <c>Auth:Authority</c> for audience <c>Auth:Audience</c>, plus
    /// permission-based authorization (<see cref="HasPermissionAttribute"/>). Access tokens are also read from the
    /// <c>access_token</c> query string, which SignalR WebSockets need.
    /// </summary>
    public static IServiceCollection AddOpenIddictExtension(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(AuthSettings.SectionName).Get<AuthSettings>() ?? new AuthSettings();
        services.Configure<AuthSettings>(configuration.GetSection(AuthSettings.SectionName));
        services.AddOpenIddict().AddValidation(options =>
        {
            if (!string.IsNullOrWhiteSpace(settings.Authority))
            {
                options.SetIssuer(settings.Authority);
            }

            if (!string.IsNullOrWhiteSpace(settings.Audience))
            {
                options.AddAudiences(settings.Audience);
            }

            options.UseSystemNetHttp();
            options.UseAspNetCore();
        });
        services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        services.AddPermissionAuthorization();
        return services;
    }

    /// <summary>Permission policies (<c>Permission:{name}</c>) checked against the <c>permission</c> claims.</summary>
    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
        return services;
    }

    /// <summary>Exposes the AppServices of <paramref name="assemblies"/> as controllers.</summary>
    public static IServiceCollection AddDynamicControllers(this IServiceCollection services, params Assembly[] assemblies)
    {
        var builder = services.AddMvcCore();
        foreach (var assembly in assemblies)
        {
            if (!builder.PartManager.ApplicationParts.OfType<AssemblyPart>().Any(p => p.Assembly == assembly))
            {
                builder.PartManager.ApplicationParts.Add(new AssemblyPart(assembly));
            }
        }

        if (!builder.PartManager.FeatureProviders.OfType<DynamicControllerFeatureProvider>().Any())
        {
            builder.PartManager.FeatureProviders.Add(new DynamicControllerFeatureProvider());
        }

        services.Configure<MvcOptions>(options => options.Conventions.Add(new DynamicControllerConvention()));
        return services;
    }

    /// <summary>All errors (including ModelState) go through <see cref="ExceptionHandlingMiddleware"/>.</summary>
    public static IServiceCollection SuppressModelStateInvalidFilter(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);
        return services;
    }

    /// <summary>JSON: camelCase, enums as strings.</summary>
    public static IServiceCollection ConfigureJsonOptions(this IServiceCollection services)
    {
        services.Configure<JsonOptions>(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        return services;
    }

    /// <summary>FluentValidation automatic validation (skipped for <see cref="Dtos.ISkipAutoValidation"/> DTOs).</summary>
    public static IServiceCollection AddFluentValidationAutoValidationService(this IServiceCollection services)
    {
        ValidatorOptions.Global.LanguageManager.Enabled = false;
        services.AddFluentValidationAutoValidation();
        return services;
    }

    /// <summary>The <c>SlidingPolicy</c> rate limiter, partitioned per user (or per IP when anonymous).</summary>
    public static IServiceCollection AddSlidingWindowRateLimiterStrategy(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(RateLimiterSettings.SectionName).Get<RateLimiterSettings>() ?? new RateLimiterSettings();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(RateLimiterSettings.PolicyName, context =>
            {
                var key = CurrentUser.GetUserId(context.User)?.ToString()
                    ?? context.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous";
                return RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = settings.PermitLimit,
                    Window = TimeSpan.FromSeconds(settings.Window),
                    SegmentsPerWindow = settings.SegmentsPerWindow,
                    QueueLimit = 0,
                });
            });
        });
        return services;
    }
}
