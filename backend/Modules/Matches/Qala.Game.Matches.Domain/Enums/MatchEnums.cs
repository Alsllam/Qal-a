namespace Qala.Game.Matches.Domain.Enums;

/// <summary>Match lifecycle. Persisted as int.</summary>
public enum MatchStatus
{
    /// <summary>A challenge waiting for a friend to accept the code.</summary>
    Waiting = 0,

    Active = 1,

    Finished = 2,

    /// <summary>Cancelled without a result (unaccepted challenge, banned player). Not rated.</summary>
    Aborted = 3,
}

/// <summary>
/// Why a match ended. The first eight values mirror <see cref="Qala.Game.Rules.EndReason"/>; the rest are
/// match-level reasons. Persisted as int; exposed in camelCase (<c>waterVictory</c>, <c>timeout</c>, …).
/// </summary>
public enum MatchEndReason
{
    AmirCaptured = 0,
    QalaTaken = 1,
    NoLegalMoves = 2,
    WaterVictory = 3,
    PlyLimitWater = 4,
    PlyLimitWells = 5,
    PlyLimitMaterial = 6,
    PlyLimitDraw = 7,
    Timeout = 100,
    Resign = 101,
    Abandon = 102,
}

public static class MatchEndReasonExtensions
{
    /// <summary>camelCase name used by the API and events.</summary>
    public static string Name(this MatchEndReason reason)
    {
        var text = reason.ToString();
        return char.ToLowerInvariant(text[0]) + text[1..];
    }

    public static MatchEndReason FromRules(Qala.Game.Rules.EndReason reason) => (MatchEndReason)(int)reason;
}

/// <summary>What happened to a submitted move.</summary>
public enum MoveStatus
{
    Accepted = 0,

    /// <summary>An idempotent retry of a move the server already played; nothing changes.</summary>
    Duplicate = 1,

    Rejected = 2,
}
