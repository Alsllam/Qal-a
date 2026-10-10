namespace Qala.Game.Rules.Tests;

/// <summary>
/// Replays every position exported by the Dart reference engine and checks that the C# port agrees
/// on legal moves, supply, the resulting position and the outcome.
/// </summary>
public class RulesParityTests
{
    private static readonly VectorFile Vectors = TestVectors.ForStandardRules;

    [Fact]
    public void RuleSet_ShouldMatchStandard_WhenBuiltFromVectorFile()
    {
        var fromFile = TestVectors.ToRuleSet(Vectors);

        Assert.Equal(RuleSet.Standard, fromFile);
    }

    [Fact]
    public void Vectors_ShouldContainPositions_WhenLoaded()
    {
        Assert.Equal("0.6", Vectors.RulesVersion);
        Assert.True(Vectors.Positions.Count > 2000, $"Only {Vectors.Positions.Count} positions");
    }

    [Fact]
    public void EveryPosition_ShouldMatchDartEngine_WhenReplayed()
    {
        var rules = TestVectors.ToRuleSet(Vectors);
        var failures = new List<string>();
        var checkedCount = 0;

        foreach (var vector in Vectors.Positions)
        {
            var state = GameState.FromNotation(vector.Position, rules);
            var problems = new List<string>();

            if (state.ToNotation() != vector.Position)
            {
                problems.Add($"round-trip notation {state.ToNotation()}");
            }

            var legal = state.LegalMoves.Select(m => m.Notation).Order(StringComparer.Ordinal).ToList();
            if (!legal.SequenceEqual(vector.LegalMoves))
            {
                problems.Add($"legal moves [{string.Join(' ', legal)}] expected [{string.Join(' ', vector.LegalMoves)}]");
            }

            var south = state.SuppliedSquares(Side.South).Select(s => s.Name).Order(StringComparer.Ordinal).ToList();
            if (!south.SequenceEqual(vector.SuppliedSouth))
            {
                problems.Add($"suppliedSouth [{string.Join(' ', south)}] expected [{string.Join(' ', vector.SuppliedSouth)}]");
            }

            var north = state.SuppliedSquares(Side.North).Select(s => s.Name).Order(StringComparer.Ordinal).ToList();
            if (!north.SequenceEqual(vector.SuppliedNorth))
            {
                problems.Add($"suppliedNorth [{string.Join(' ', north)}] expected [{string.Join(' ', vector.SuppliedNorth)}]");
            }

            var next = state.Play(Move.Parse(vector.Move));
            if (next.ToNotation() != vector.After)
            {
                problems.Add($"after {next.ToNotation()} expected {vector.After}");
            }

            var outcome = next.Outcome is null
                ? null
                : new VectorOutcome(next.Outcome.Winner?.Name(), next.Outcome.Reason.Name());
            if (outcome != vector.Outcome)
            {
                problems.Add($"outcome {outcome} expected {vector.Outcome}");
            }

            if (problems.Count > 0)
            {
                failures.Add($"{vector.Position} / {vector.Move}: {string.Join("; ", problems)}");
            }

            checkedCount++;
        }

        Assert.Equal(Vectors.Positions.Count, checkedCount);
        Assert.True(failures.Count == 0, $"{failures.Count}/{checkedCount} positions differ:\n{string.Join('\n', failures.Take(20))}");
    }

    [Fact]
    public void EveryRecordedGame_ShouldChainPositions_WhenPlayedFromTheStart()
    {
        // Each vector's "after" is the next vector's "position" until a game ends, so the file is a set of
        // complete games. Replaying them move by move from the opening must reach every recorded outcome.
        var rules = TestVectors.ToRuleSet(Vectors);
        GameState? state = null;
        var games = 0;

        foreach (var vector in Vectors.Positions)
        {
            state ??= GameState.Initial(rules);
            Assert.Equal(vector.Position, state.ToNotation());
            state = state.Play(Move.Parse(vector.Move));
            if (state.IsOver)
            {
                Assert.NotNull(vector.Outcome);
                games++;
                state = null;
            }
        }

        Assert.Null(state);
        Assert.Equal(40, games);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Perft_ShouldMatchDartEngine_WhenSearchedFromOpening(int depth)
    {
        var rules = TestVectors.ToRuleSet(Vectors);

        var nodes = GameState.Perft(GameState.Initial(rules), depth);

        Assert.Equal(Vectors.Perft[depth - 1], nodes);
    }
}
