using Qala.Game.Matches.Application.Stats;
using Qala.Game.Matches.Domain.Enums;
using Qala.Game.Matches.Domain.Repositories;
using Qala.Game.Rules;

namespace Qala.Game.Matches.Tests.Application.Stats;

public class BalanceStatsCalculatorTests
{
    private static readonly DateTime Day1 = new(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Day2 = new(2026, 10, 2, 22, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Calculate_ShouldComputeSouthScoreDrawsLengthReasonsAndDays_WhenGivenMatches()
    {
        // Arrange: South wins 2, North wins 1, 1 draw.
        FinishedMatchSummary[] matches =
        [
            new(Day1, Side.South, MatchEndReason.WaterVictory, 40),
            new(Day1, Side.South, MatchEndReason.AmirCaptured, 30),
            new(Day2, Side.North, MatchEndReason.WaterVictory, 50),
            new(Day2, null, MatchEndReason.PlyLimitDraw, 60),
        ];

        // Act
        var stats = BalanceStatsCalculator.Calculate(Day1.Date, Day2.Date.AddDays(1), "0.6", matches);

        // Assert
        Assert.Equal(4, stats.Games);
        Assert.Equal(2, stats.SouthWins);
        Assert.Equal(1, stats.NorthWins);
        Assert.Equal(1, stats.Draws);
        Assert.Equal(0.625, stats.SouthScore);
        Assert.Equal(0.25, stats.DrawRate);
        Assert.Equal(45, stats.MeanPlies);
        Assert.Equal("waterVictory", stats.EndReasons[0].Reason);
        Assert.Equal(2, stats.EndReasons[0].Count);
        Assert.Equal(0.5, stats.EndReasons[0].Share);
        Assert.Equal(2, stats.ByDay.Count);
        Assert.Equal(new DateOnly(2026, 10, 1), stats.ByDay[0].Date);
        Assert.Equal(1.0, stats.ByDay[0].SouthScore);
        Assert.Equal(0.25, stats.ByDay[1].SouthScore);
        Assert.Equal(55, stats.ByDay[1].MeanPlies);
        Assert.Equal(0, stats.ByDay[0].Draws);
        Assert.Equal(1, stats.ByDay[1].Draws);
        Assert.Equal(45, stats.MedianPlies);
    }

    [Fact]
    public void Calculate_ShouldBucketGameLengths_WhenGivenMatches()
    {
        // Arrange
        FinishedMatchSummary[] matches =
        [
            new(Day1, Side.South, MatchEndReason.AmirCaptured, 7),
            new(Day1, Side.South, MatchEndReason.WaterVictory, 19),
            new(Day1, Side.North, MatchEndReason.WaterVictory, 20),
            new(Day1, null, MatchEndReason.PlyLimitDraw, 60),
            new(Day1, Side.North, MatchEndReason.PlyLimitWater, 60),
        ];

        // Act
        var stats = BalanceStatsCalculator.Calculate(Day1.Date, Day1.Date.AddDays(1), "0.6", matches);

        // Assert: 1–9, 10–19, 20–29, 30–39, 40–49, 50–59, 60.
        Assert.Equal([1, 1, 1, 0, 0, 0, 2], stats.LengthHistogram.Select(b => b.Count));
        Assert.Equal((1, 9), (stats.LengthHistogram[0].FromPly, stats.LengthHistogram[0].ToPly));
        Assert.Equal((60, 60), (stats.LengthHistogram[^1].FromPly, stats.LengthHistogram[^1].ToPly));
        Assert.Equal(matches.Length, stats.LengthHistogram.Sum(b => b.Count));
        Assert.Equal(20, stats.MedianPlies);
    }

    [Fact]
    public void Calculate_ShouldReturnZeros_WhenNoMatches()
    {
        var stats = BalanceStatsCalculator.Calculate(Day1, Day2, null, []);

        Assert.Equal(0, stats.Games);
        Assert.Equal(0, stats.SouthScore);
        Assert.Empty(stats.EndReasons);
        Assert.Empty(stats.ByDay);
    }
}
