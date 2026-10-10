using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Qala.Framework.Application.Localization;
using Qala.Framework.Domain.Exceptions;
using DotNetValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace Qala.Framework.Application.ExceptionHandling;

/// <summary>The only error shape the API returns.</summary>
public sealed record ErrorResponse(ErrorBody Error);

public sealed record ErrorBody(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("date")] DateTime Date,
    [property: JsonPropertyName("messages")] IReadOnlyList<string> Messages,
    [property: JsonPropertyName("source")] string Source);

/// <summary>Where an error came from.</summary>
public static class ErrorSources
{
    public const string Validation = "Validation";
    public const string Application = "Application";
    public const string Binding = "Binding";
    public const string Parsing = "Parsing";
}

/// <summary>The request body could not be bound (bad JSON, wrong types). Mapped to 400.</summary>
public sealed class RequestBindingException(IReadOnlyList<string> messages, string source) : Exception(string.Join(" ", messages))
{
    public IReadOnlyList<string> Messages { get; } = messages;

    public string ErrorSource { get; } = source;
}

/// <summary>
/// Turns every exception into <c>{ "error": { "code", "date", "messages", "source" } }</c>. Messages that are
/// localization keys are translated; unexpected errors get a generic message and are logged, never echoed.
/// </summary>
public sealed partial class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IStringLocalizer localizer, TimeProvider timeProvider)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client went away; nothing to send.
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                LogAfterResponseStarted(logger, exception);
                throw;
            }

            var (status, messages, source) = Map(exception);
            if (status >= HttpStatusCode.InternalServerError)
            {
                LogUnhandled(logger, exception, context.Request.Path);
            }

            var body = new ErrorResponse(new ErrorBody(((int)status).ToString(System.Globalization.CultureInfo.InvariantCulture), timeProvider.GetUtcNow().UtcDateTime, messages, source));
            context.Response.Clear();
            context.Response.StatusCode = (int)status;
            context.Response.ContentType = "application/json; charset=utf-8";
            await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions), context.RequestAborted);
        }
    }

    /// <summary>Maps an exception to a status code, localized messages and a source.</summary>
    public (HttpStatusCode Status, IReadOnlyList<string> Messages, string Source) Map(Exception exception) => exception switch
    {
        ValidationException e => (HttpStatusCode.BadRequest, ValidationMessages(e), ErrorSources.Validation),
        CustomValidationException e => (HttpStatusCode.BadRequest, Localize(e.Messages.Count > 0 ? e.Messages : [e.Message]), ErrorSources.Validation),
        DotNetValidationException e => (HttpStatusCode.BadRequest, Localize([e.Message]), ErrorSources.Validation),
        RequestBindingException e => (HttpStatusCode.BadRequest, Localize(e.Messages), e.ErrorSource),
        BadHttpRequestException or JsonException => (HttpStatusCode.BadRequest, Localize([LocalizationKeys.InvalidRequest]), ErrorSources.Parsing),
        EntityNotFoundException e => (HttpStatusCode.NotFound, Localize([e.EntityType is null ? e.Message : LocalizationKeys.NotFound]), ErrorSources.Application),
        NotFoundException e => (HttpStatusCode.NotFound, Localize([e.Message]), ErrorSources.Application),
        ForbiddenException e => (HttpStatusCode.Forbidden, Localize([string.IsNullOrEmpty(e.Message) ? LocalizationKeys.Forbidden : e.Message]), ErrorSources.Application),
        UnauthorizedAccessException => (HttpStatusCode.Unauthorized, Localize([LocalizationKeys.Unauthorized]), ErrorSources.Application),
        ServiceUnAvailableException => (HttpStatusCode.ServiceUnavailable, Localize([LocalizationKeys.ServiceUnavailable]), ErrorSources.Application),
        UserFriendlyException e => (e.HttpStatusCode, Localize([e.Message]), ErrorSources.Application),
        DbUpdateConcurrencyException => (HttpStatusCode.Conflict, Localize([LocalizationKeys.ConcurrencyConflict]), ErrorSources.Application),
        _ => (HttpStatusCode.InternalServerError, Localize([LocalizationKeys.UnexpectedError]), ErrorSources.Application),
    };

    private List<string> ValidationMessages(ValidationException exception)
    {
        if (!exception.Errors.Any())
        {
            return Localize([exception.Message]);
        }

        return exception.Errors
            .Select(e =>
            {
                var field = localizer[$"Fields:{e.PropertyName}"];
                var name = field.ResourceNotFound ? e.PropertyName : field.Value;
                return string.IsNullOrEmpty(name) ? localizer[e.ErrorMessage].Value : $"{name}: {localizer[e.ErrorMessage].Value}";
            })
            .Distinct()
            .ToList();
    }

    private List<string> Localize(IEnumerable<string> messages) => messages.Select(m => localizer[m].Value).ToList();

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception for {Path}")]
    private static partial void LogUnhandled(ILogger logger, Exception exception, PathString path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Exception after the response started; rethrowing")]
    private static partial void LogAfterResponseStarted(ILogger logger, Exception exception);
}
