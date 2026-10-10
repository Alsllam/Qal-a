namespace Qala.Game.Matches.Application.Matches.DTOs;

/// <summary>The live balance dashboard: the same metrics as the balance lab (docs/balance-log.md).</summary>
public sealed class BalanceStatsDto
{
    public DateTime From { get; init; }

    public DateTime To { get; init; }

    public string? RulesVersion { get; init; }

    public int Games { get; init; }

    public int SouthWins { get; init; }

    public int NorthWins { get; init; }

    public int Draws { get; init; }

    /// <summary>South's score, 0–1: (wins + draws / 2) / games.</summary>
    public double SouthScore { get; init; }

    public double DrawRate { get; init; }

    public double MeanPlies { get; init; }

    public double MedianPlies { get; init; }

    /// <summary>Game length buckets (inclusive): 1–9, 10–19, … 50–59, then games that reached 60+ plies.</summary>
    public IReadOnlyList<LengthBucketDto> LengthHistogram { get; init; } = [];

    public IReadOnlyList<EndReasonCountDto> EndReasons { get; init; } = [];

    public IReadOnlyList<DailyStatsDto> ByDay { get; init; } = [];
}

/// <summary>How often a game ended for <see cref="Reason"/> (camelCase).</summary>
public sealed record EndReasonCountDto(string Reason, int Count, double Share);

/// <summary>One UTC day.</summary>
public sealed record DailyStatsDto(DateOnly Date, int Games, double SouthScore, int Draws, double MeanPlies);

/// <summary>Number of games whose length was between <see cref="FromPly"/> and <see cref="ToPly"/> plies.</summary>
public sealed record LengthBucketDto(int FromPly, int ToPly, int Count);
