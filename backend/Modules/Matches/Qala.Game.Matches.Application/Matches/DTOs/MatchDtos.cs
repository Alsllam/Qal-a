using Qala.Game.Matches.Domain.Enums;

namespace Qala.Game.Matches.Application.Matches.DTOs;

/// <summary>A participant. <see cref="PlayerId"/> is the auth user id.</summary>
public sealed record MatchPlayerDto(Guid PlayerId, string DisplayName, double? Rating);

/// <summary>Remaining time per side at the moment the DTO was built (the side to move's clock is running).</summary>
public sealed record MatchClocksDto(long SouthMs, long NorthMs, long IncrementMs);

/// <summary><see cref="Winner"/> is <c>south</c>, <c>north</c> or null (draw); <see cref="Reason"/> is camelCase.</summary>
public sealed record MatchOutcomeDto(string? Winner, string Reason);

/// <summary>Full match state (docs/architecture.md §5).</summary>
public sealed class MatchDto
{
    public Guid Id { get; init; }

    public string RulesVersion { get; init; } = string.Empty;

    public MatchStatus Status { get; init; }

    public MatchPlayerDto? South { get; init; }

    public MatchPlayerDto? North { get; init; }

    /// <summary>Rules notation, water points included.</summary>
    public string Position { get; init; } = string.Empty;

    public int Ply { get; init; }

    /// <summary><c>south</c> or <c>north</c>.</summary>
    public string ToMove { get; init; } = string.Empty;

    public IReadOnlyList<string> Moves { get; init; } = [];

    public MatchClocksDto Clocks { get; init; } = new(0, 0, 0);

    /// <summary><c>minutes+seconds</c>, e.g. <c>4+2</c>.</summary>
    public string TimeControl { get; init; } = string.Empty;

    /// <summary>Only shown to the creator while the challenge is waiting.</summary>
    public string? ChallengeCode { get; init; }

    public DateTime? StartedAt { get; init; }

    public DateTime? FinishedAt { get; init; }

    public MatchOutcomeDto? Outcome { get; init; }
}

/// <summary>A row of <c>POST list</c> / <c>POST mine</c>.</summary>
public sealed class MatchListDto
{
    public Guid Id { get; init; }

    public string RulesVersion { get; init; } = string.Empty;

    public MatchStatus Status { get; init; }

    public Guid? SouthPlayerId { get; init; }

    public string? SouthDisplayName { get; init; }

    public Guid? NorthPlayerId { get; init; }

    public string? NorthDisplayName { get; init; }

    public int Plies { get; init; }

    public MatchOutcomeDto? Outcome { get; init; }

    public DateTime CreationTime { get; init; }

    public DateTime? StartedAt { get; init; }

    public DateTime? FinishedAt { get; init; }
}

/// <summary>Pushed to both players when a move is accepted.</summary>
public sealed record MoveMadeDto(Guid MatchId, string Move, int Ply, string Position, MatchClocksDto Clocks, MatchOutcomeDto? Outcome);

/// <summary>Sent to the caller when a move is refused. <see cref="Code"/> is the localization key.</summary>
public sealed record MoveRejectedDto(Guid MatchId, string Code, string Reason, int ServerPly);

/// <summary>Pushed to a player when matchmaking (or an accepted challenge) starts a match.</summary>
public sealed record MatchFoundDto(Guid MatchId, string Side);

/// <summary>Pushed to both players when a match ends. <see cref="Outcome"/> is null for an aborted match.</summary>
public sealed record MatchEndedDto(Guid MatchId, MatchOutcomeDto? Outcome);

/// <summary>Pushed to the match group when a player offers a rematch.</summary>
public sealed record RematchOfferedDto(Guid MatchId, Guid ByPlayerId);

/// <summary>What happened to a <c>MakeMove</c>.</summary>
public sealed record MakeMoveResultDto(MoveStatus Status, string? ErrorKey, MoveMadeDto? MoveMade);
