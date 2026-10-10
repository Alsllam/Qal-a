using FluentValidation;
using Qala.Framework.Application.Localization;
using Qala.Framework.Domain.Constants;
using Qala.Game.Players.Application.Players.DTOs;
using Qala.Game.Players.Domain.Constants;

namespace Qala.Game.Players.Application.Players.Validations;

public sealed class UpdateMyProfileValidator : AbstractValidator<UpdateMyProfileDto>
{
    public UpdateMyProfileValidator() =>
        When(x => !x.SkipAutoValidations, () =>
        {
            RuleFor(x => x.DisplayName)
                .NotEmpty().WithMessage(LocalizationKeys.Required)
                .MinimumLength(FieldDefinitions.MinDisplayNameLength).WithMessage(LocalizationKeys.MinLength)
                .MaximumLength(FieldDefinitions.MaxDisplayNameLength).WithMessage(LocalizationKeys.MaxLength)
                .Matches(FieldDefinitions.DisplayNamePattern).WithMessage(LocalizationKeys.InvalidCharacters);
            RuleFor(x => x.AvatarId)
                .MaximumLength(FieldDefinitions.MaxAvatarIdLength).WithMessage(LocalizationKeys.MaxLength);
            RuleFor(x => x.Locale)
                .NotEmpty().WithMessage(LocalizationKeys.Required)
                .Must(l => FieldDefinitions.SupportedLocales.Contains(l)).WithMessage(PlayerErrors.UnsupportedLocale);
        });
}
