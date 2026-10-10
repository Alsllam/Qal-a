using Qala.Game.Matches.Application.Matches.DTOs;
using Qala.Game.Matches.Domain.Enums;
using Qala.Game.Matches.Domain.Repositories;
using Qala.Game.Rules;

namespace Qala.Game.Matches.Application.Stats;

/// <summary>Aggregates finished matches into the balance-lab metrics (South score, draws, length, end reasons, per day).</summary>
public static class BalanceStatsCalculator
{
    public static BalanceStatsDto Calculate(DateTime from, DateTime to, string? rulesVersion, IReadOnlyCollection<FinishedMatchSummary> matches)
    {
        ArgumentNullException.ThrowIfNull(matches);
        var games = matches.Count;
        var southWins = matches.Count(m => m.Winner == Side.South);
        var northWins = matches.Count(m => m.Winner == Side.North);
        var draws = games - southWins - northWins;
        return new BalanceStatsDto
        {
            From = from,
            To = to,
            RulesVersion = rulesVersion,
            Games = games,
            SouthWins = southWins,
            NorthWins = northWins,
            Draws = draws,
            SouthScore = Score(southWins, draws, games),
            DrawRate = games == 0 ? 0 : Math.Round((double)draws / games, 4),
            MeanPlies = games == 0 ? 0 : Math.Round(matches.Average(m => m.Plies), 2),
            MedianPlies = Median(matches.Select(m => m.Plies)),
            LengthHistogram = Histogram(matches.Select(m => m.Plies)),
            EndReasons = matches
                .GroupBy(m => m.Reason)
                .Select(g => new EndReasonCountDto(g.Key.Name(), g.Count(), Math.Round((double)g.Count() / games, 4)))
                .OrderByDescending(r => r.Count)
                .ThenBy(r => r.Reason, StringComparer.Ordinal)
                .ToList(),
            ByDay = matches
                .GroupBy(m => DateOnly.FromDateTime(m.FinishedAt))
                .OrderBy(g => g.Key)
                .Select(g => new DailyStatsDto(
                    g.Key,
                    g.Count(),
                    Score(g.Count(m => m.Winner == Side.South), g.Count(m => m.Winner is null), g.Count()),
                    g.Count(m => m.Winner is null),
                    Math.Round(g.Average(m => m.Plies), 2)))
                .ToList(),
        };
    }

    /// <summary>The ply limit of the current rules; longer games (other versions) fall in the last bucket.</summary>
    private const int LastBucketFrom = 60;

    private static double Median(IEnumerable<int> values)
    {
        var sorted = values.Order().ToArray();
        if (sorted.Length == 0)
        {
            return 0;
        }

        var mid = sorted.Length / 2;
        return sorted.Length % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2.0;
    }

    private static List<LengthBucketDto> Histogram(IEnumerable<int> plies)
    {
        var list = plies.ToList();
        var buckets = new List<LengthBucketDto>
        {
            new(1, 9, list.Count(p => p < 10)),
        };
        for (var from = 10; from < LastBucketFrom; from += 10)
        {
            var start = from;
            buckets.Add(new LengthBucketDto(start, start + 9, list.Count(p => p >= start && p <= start + 9)));
        }

        buckets.Add(new LengthBucketDto(LastBucketFrom, LastBucketFrom, list.Count(p => p >= LastBucketFrom)));
        return buckets;
    }

    private static double Score(int wins, int draws, int games) =>
        games == 0 ? 0 : Math.Round((wins + draws / 2.0) / games, 4);
}
