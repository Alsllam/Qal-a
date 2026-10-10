using FluentValidation;
using Qala.Framework.Application.Dtos;

namespace Qala.Framework.Application.Validation;

public static class ValidationExtensions
{
    /// <summary>Validates an <see cref="ISkipAutoValidation"/> DTO explicitly and throws <see cref="ValidationException"/> when invalid.</summary>
    public static async Task ValidateInputAsync<T>(this IValidator<T> validator, T input, CancellationToken cancellationToken = default)
        where T : ISkipAutoValidation
    {
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(input);
        input.SkipAutoValidations = false;
        var result = await validator.ValidateAsync(input, cancellationToken);
        if (!result.IsValid)
        {
            throw new ValidationException(result.Errors);
        }
    }
}
