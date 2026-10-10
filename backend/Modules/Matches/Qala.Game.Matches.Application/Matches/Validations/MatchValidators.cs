using System.Text.RegularExpressions;
using FluentValidation;
using Qala.Framework.Application.Localization;
using Qala.Game.Matches.Application.Matches.DTOs;
using Qala.Game.Matches.Domain.Constants;
using Qala.Game.Matches.Domain.ValueObjects;

namespace Qala.Game.Matches.Application.Matches.Validations;

public sealed class CreateChallengeValidator : AbstractValidator<CreateChallengeDto>
{
    public CreateChallengeValidator() =>
        When(x => !x.SkipAutoValidations, () =>
            RuleFor(x => x.TimeControl)
                .Matches(MatchConsts.TimeControlPattern).WithMessage(MatchErrors.InvalidTimeControl)
                .Must(t => TimeControl.TryParse(t, out _)).WithMessage(MatchErrors.InvalidTimeControl));
}

public sealed class QueueRequestValidator : AbstractValidator<QueueRequestDto>
{
    public QueueRequestValidator() =>
        When(x => !x.SkipAutoValidations, () =>
            RuleFor(x => x.TimeControl)
                .Matches(MatchConsts.TimeControlPattern).WithMessage(MatchErrors.InvalidTimeControl)
                .Must(t => TimeControl.TryParse(t, out _)).WithMessage(MatchErrors.InvalidTimeControl));
}

public sealed class AcceptChallengeValidator : AbstractValidator<AcceptChallengeDto>
{
    public AcceptChallengeValidator() =>
        When(x => !x.SkipAutoValidations, () =>
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage(LocalizationKeys.Required)
                .Must(c => Regex.IsMatch(c.Trim().ToUpperInvariant(), MatchConsts.ChallengeCodePattern, RegexOptions.None, TimeSpan.FromSeconds(1)))
                .WithMessage(MatchErrors.InvalidChallengeCode));
}

public sealed class MatchStatsRequestValidator : AbstractValidator<MatchStatsRequestDto>
{
    public MatchStatsRequestValidator() =>
        When(x => !x.SkipAutoValidations, () =>
        {
            RuleFor(x => x.To)
                .GreaterThan(x => x.From).WithMessage(MatchErrors.InvalidStatsRange)
                .Must((dto, to) => (to - dto.From).TotalDays <= MatchConsts.MaxStatsRangeDays).WithMessage(MatchErrors.InvalidStatsRange);
            RuleFor(x => x.RulesVersion)
                .Must(v => v is null || Qala.Game.Rules.RuleSet.Published.ContainsKey(v)).WithMessage(LocalizationKeys.InvalidValue);
        });
}
