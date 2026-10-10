using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Qala.Framework.Application.ExceptionHandling;

/// <summary>
/// With <c>SuppressModelStateInvalidFilter</c> on, this filter turns an invalid ModelState into an exception, so every
/// error goes through <see cref="ExceptionHandlingMiddleware"/>. JSON errors (keys starting with <c>$</c>) are
/// <c>Parsing</c>; the rest (FluentValidation auto-validation) are <c>Validation</c>.
/// </summary>
public sealed class ModelStateValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (context.ModelState.IsValid)
        {
            return;
        }

        var entries = context.ModelState.Where(e => e.Value is { Errors.Count: > 0 }).ToList();
        if (entries.Any(e => e.Key.StartsWith('$') || e.Value!.Errors.Any(x => x.Exception is not null)))
        {
            throw new RequestBindingException([Localization.LocalizationKeys.InvalidRequest], ErrorSources.Parsing);
        }

        var failures = entries
            .SelectMany(e => e.Value!.Errors.Select(x => new ValidationFailure(e.Key, x.ErrorMessage)))
            .ToList();
        throw new ValidationException(failures);
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
