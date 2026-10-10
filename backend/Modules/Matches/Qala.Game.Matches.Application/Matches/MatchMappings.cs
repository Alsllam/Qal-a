using Qala.Game.Matches.Application.Matches.DTOs;
using Qala.Game.Matches.Domain.Entities;
using Qala.Game.Matches.Domain.Enums;
using Qala.Game.Rules;

namespace Qala.Game.Matches.Application.Matches;

/// <summary>Entity → DTO maps (explicit code instead of AutoMapper; see backend/README.md).</summary>
public static class MatchMappings
{
    public static MatchDto ToDto(this Match match, DateTime now, Guid? viewerId = null)
    {
        ArgumentNullException.ThrowIfNull(match);
        return new MatchDto
        {
            Id = match.Id,
            RulesVersion = match.RulesVersion,
            Status = match.Status,
            South = match.SouthPlayerId is { } south ? new MatchPlayerDto(south, match.SouthDisplayName ?? string.Empty, match.SouthRating) : null,
            North = match.NorthPlayerId is { } north ? new MatchPlayerDto(north, match.NorthDisplayName ?? string.Empty, match.NorthRating) : null,
            Position = match.Position,
            Ply = match.Ply,
            ToMove = match.SideToMove.Name(),
            Moves = match.OrderedMoves().Select(m => m.Notation).ToList(),
            Clocks = match.ToClocksDto(now),
            TimeControl = match.TimeControl.ToString(),
            ChallengeCode = viewerId == match.CreatorPlayerId ? match.ChallengeCode : null,
            StartedAt = match.StartedAt,
            FinishedAt = match.FinishedAt,
            Outcome = match.ToOutcomeDto(),
        };
    }

    public static MatchClocksDto ToClocksDto(this Match match, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(match);
        return new MatchClocksDto(match.RemainingMs(Side.South, now), match.RemainingMs(Side.North, now), match.IncrementMs);
    }

    public static MatchOutcomeDto? ToOutcomeDto(this Match match)
    {
        ArgumentNullException.ThrowIfNull(match);
        return match.Status == MatchStatus.Finished && match.EndReason is { } reason
            ? new MatchOutcomeDto(match.Winner?.Name(), reason.Name())
            : null;
    }

    public static MatchListDto ToListDto(this Match match)
    {
        ArgumentNullException.ThrowIfNull(match);
        return new MatchListDto
        {
            Id = match.Id,
            RulesVersion = match.RulesVersion,
            Status = match.Status,
            SouthPlayerId = match.SouthPlayerId,
            SouthDisplayName = match.SouthDisplayName,
            NorthPlayerId = match.NorthPlayerId,
            NorthDisplayName = match.NorthDisplayName,
            Plies = match.Ply,
            Outcome = match.ToOutcomeDto(),
            CreationTime = match.CreationTime,
            StartedAt = match.StartedAt,
            FinishedAt = match.FinishedAt,
        };
    }
}
