using Qala.Framework.Domain.Constants;
using Qala.Framework.Domain.Entities;

namespace Qala.Game.Matches.Domain.Entities;

/// <summary>One ply of a match, in order.</summary>
public class MatchMove : Entity<Guid>
{
    public const int MaxNotationLength = FieldDefinitions.MaxMoveLength;

    protected MatchMove()
    {
    }

    internal MatchMove(Guid matchId, int ply, string notation, DateTime playedAt, long clockAfterMs)
        : base(Guid.NewGuid())
    {
        MatchId = matchId;
        Ply = ply;
        Notation = notation;
        PlayedAt = playedAt;
        ClockAfterMs = clockAfterMs;
    }

    public Guid MatchId { get; private set; }

    /// <summary>Plies played before this move (0 for South's first move).</summary>
    public int Ply { get; private set; }

    /// <summary>Move notation, e.g. <c>c2-c3</c>.</summary>
    public string Notation { get; private set; } = string.Empty;

    public DateTime PlayedAt { get; private set; }

    /// <summary>The mover's clock after the move (increment included).</summary>
    public long ClockAfterMs { get; private set; }
}
