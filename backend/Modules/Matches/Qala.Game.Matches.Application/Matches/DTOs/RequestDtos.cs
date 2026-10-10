using System.Text.Json.Serialization;
using Qala.Framework.Application.Dtos;
using Qala.Game.Matches.Domain.Constants;
using Qala.Game.Matches.Domain.Enums;

namespace Qala.Game.Matches.Application.Matches.DTOs;

/// <summary><c>POST challenge</c>: create a friend challenge.</summary>
public sealed class CreateChallengeDto : ISkipAutoValidation
{
    /// <summary><c>minutes+seconds</c>; default <c>4+2</c>.</summary>
    public string TimeControl { get; set; } = MatchConsts.DefaultTimeControl;

    [JsonIgnore]
    public bool SkipAutoValidations { get; set; } = true;
}

/// <summary>The code to share with a friend.</summary>
public sealed record ChallengeCodeDto(Guid MatchId, string Code);

/// <summary><c>POST challenge/accept</c>.</summary>
public sealed class AcceptChallengeDto : ISkipAutoValidation
{
    public string Code { get; set; } = string.Empty;

    [JsonIgnore]
    public bool SkipAutoValidations { get; set; } = true;
}

/// <summary><c>POST queue</c>: join matchmaking.</summary>
public sealed class QueueRequestDto : ISkipAutoValidation
{
    public string TimeControl { get; set; } = MatchConsts.DefaultTimeControl;

    [JsonIgnore]
    public bool SkipAutoValidations { get; set; } = true;
}

/// <summary>A matchmaking ticket (<c>POST queue</c> result, <c>DELETE queue</c> body).</summary>
public sealed class QueueTicketDto
{
    public Guid TicketId { get; set; }

    /// <summary>Set when the ticket was paired at once; the match is also pushed through the hub.</summary>
    public Guid? MatchId { get; set; }
}

/// <summary><c>POST mine</c>.</summary>
public sealed class FilterMyMatchesDto : PagedRequestDto
{
    public MatchStatus? Status { get; set; }
}

/// <summary><c>POST list</c> (admin).</summary>
public sealed class FilterMatchDto : BaseFilterRequestDto
{
    public MatchStatus? Status { get; set; }

    public string? RulesVersion { get; set; }

    public Guid? PlayerId { get; set; }

    public DateTime? From { get; set; }

    public DateTime? To { get; set; }
}

/// <summary><c>POST stats</c>: <c>from &lt;= finishedAt &lt; to</c>.</summary>
public sealed class MatchStatsRequestDto : ISkipAutoValidation
{
    public DateTime From { get; set; }

    public DateTime To { get; set; }

    public string? RulesVersion { get; set; }

    [JsonIgnore]
    public bool SkipAutoValidations { get; set; } = true;
}
