using System.Globalization;
using System.Net;
using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Qala.Framework.Application.ExceptionHandling;
using Qala.Framework.Application.Localization;
using Qala.Framework.Domain.Exceptions;

namespace Qala.Framework.Tests;

public class ExceptionHandlingMiddlewareTests
{
    private static readonly JsonLocalizationStore Store = new([typeof(LocalizationKeys).Assembly, typeof(Qala.Game.Matches.Application.MatchesApplicationModule).Assembly]);

    public static TheoryData<Exception, HttpStatusCode, string> Cases => new()
    {
        { new ValidationException([new ValidationFailure("TimeControl", "Matches:Errors:InvalidTimeControl")]), HttpStatusCode.BadRequest, "Validation" },
        { new CustomValidationException("General:Business:DeleteNotAllowed"), HttpStatusCode.BadRequest, "Validation" },
        { new EntityNotFoundException(typeof(object), 1), HttpStatusCode.NotFound, "Application" },
        { new NotFoundException("General:Business:NotFound"), HttpStatusCode.NotFound, "Application" },
        { new ForbiddenException(), HttpStatusCode.Forbidden, "Application" },
        { new ServiceUnAvailableException(), HttpStatusCode.ServiceUnavailable, "Application" },
        { new UserFriendlyException("General:Errors:RateLimited", HttpStatusCode.TooManyRequests), HttpStatusCode.TooManyRequests, "Application" },
        { new RequestBindingException(["General:Errors:InvalidRequest"], ErrorSources.Parsing), HttpStatusCode.BadRequest, "Parsing" },
        { new InvalidOperationException("secret stack details"), HttpStatusCode.InternalServerError, "Application" },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task Invoke_ShouldWriteStandardErrorShape_WhenExceptionIsThrown(Exception exception, HttpStatusCode status, string source)
    {
        // Arrange
        var (context, middleware) = Build(exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal((int)status, context.Response.StatusCode);
        using var json = await ReadAsync(context);
        var error = json.RootElement.GetProperty("error");
        Assert.Equal(((int)status).ToString(CultureInfo.InvariantCulture), error.GetProperty("code").GetString());
        Assert.Equal(source, error.GetProperty("source").GetString());
        Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), error.GetProperty("date").GetDateTime());
        var messages = error.GetProperty("messages").EnumerateArray().Select(m => m.GetString()!).ToList();
        Assert.NotEmpty(messages);
        Assert.DoesNotContain(messages, m => m.Contains("secret", StringComparison.Ordinal));
        Assert.DoesNotContain(messages, m => m.StartsWith("General:", StringComparison.Ordinal) || m.StartsWith("Matches:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Invoke_ShouldLocalizeToArabic_WhenUiCultureIsArabic()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("ar");
            var (context, middleware) = Build(new ValidationException([new ValidationFailure("TimeControl", "Matches:Errors:InvalidTimeControl")]));

            await middleware.InvokeAsync(context);

            using var json = await ReadAsync(context);
            var message = json.RootElement.GetProperty("error").GetProperty("messages")[0].GetString();
            Assert.Equal("نظام الوقت: يجب أن يكون نظام الوقت مثل 4+2 (دقائق + ثوانٍ).", message);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    private static (DefaultHttpContext Context, ExceptionHandlingMiddleware Middleware) Build(Exception exception)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var middleware = new ExceptionHandlingMiddleware(_ => throw exception, NullLogger<ExceptionHandlingMiddleware>.Instance, new JsonStringLocalizer(Store), clock);
        return (context, middleware);
    }

    private static async Task<JsonDocument> ReadAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return await JsonDocument.ParseAsync(context.Response.Body);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
