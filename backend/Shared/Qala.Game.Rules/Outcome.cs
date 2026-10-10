namespace Qala.Game.Rules;

/// <summary>Why a game ended (rules reasons only; resign/timeout/abandon live in the Matches module).</summary>
public enum EndReason
{
    /// <summary>The winner captured (or shot) the enemy Amir.</summary>
    AmirCaptured = 0,

    /// <summary>The winner ended a turn with a supplied piece on the enemy Qal'a.</summary>
    QalaTaken = 1,

    /// <summary>The loser had no legal move on their turn.</summary>
    NoLegalMoves = 2,

    /// <summary>The winner reached <see cref="RuleSet.WaterToWin"/> water points.</summary>
    WaterVictory = 3,

    /// <summary>Ply limit reached; the winner had more water points.</summary>
    PlyLimitWater = 4,

    /// <summary>Ply limit reached; the winner held more Wells.</summary>
    PlyLimitWells = 5,

    /// <summary>Ply limit reached; Wells tied, the winner had more pieces.</summary>
    PlyLimitMaterial = 6,

    /// <summary>Ply limit reached with Wells and pieces tied.</summary>
    PlyLimitDraw = 7,
}

public static class EndReasonExtensions
{
    /// <summary>The camelCase name used by the Dart engine, the test vectors and the API (e.g. <c>waterVictory</c>).</summary>
    public static string Name(this EndReason reason)
    {
        var text = reason.ToString();
        return char.ToLowerInvariant(text[0]) + text[1..];
    }
}

/// <summary>The result of a finished game. <see cref="Winner"/> is null for a draw.</summary>
public sealed record Outcome(Side? Winner, EndReason Reason)
{
    public bool IsDraw => Winner is null;

    public override string ToString() =>
        Winner is { } winner ? $"{winner.Name()} wins ({Reason.Name()})" : $"Draw ({Reason.Name()})";
}
