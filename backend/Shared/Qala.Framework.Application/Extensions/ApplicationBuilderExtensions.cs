using System.Globalization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Qala.Framework.Application.ExceptionHandling;
using Qala.Framework.Domain.Constants;
using Serilog;

namespace Qala.Framework.Application.Extensions;

public static class ApplicationBuilderExtensions
{
    /// <summary>Picks the language (<c>ar</c> or <c>en</c>, default <c>en</c>) from <c>Accept-Language</c>.</summary>
    public static IApplicationBuilder UseLocalizationMiddleware(this IApplicationBuilder app)
    {
        var cultures = FieldDefinitions.SupportedLocales.Select(c => new CultureInfo(c)).ToList();
        return app.UseRequestLocalization(new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture("en"),
            SupportedCultures = cultures,
            SupportedUICultures = cultures,
            RequestCultureProviders = [new AcceptLanguageHeaderRequestCultureProvider()],
        });
    }

    /// <summary>One structured log line per request.</summary>
    public static IApplicationBuilder UseLoggingMiddleware(this IApplicationBuilder app) => app.UseSerilogRequestLogging();

    /// <summary>Maps every exception to the standard error shape.</summary>
    public static IApplicationBuilder UseExceptionHandlingMiddleware(this IApplicationBuilder app) =>
        app.UseMiddleware<ExceptionHandlingMiddleware>();
}
