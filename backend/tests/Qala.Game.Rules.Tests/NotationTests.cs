namespace Qala.Game.Rules.Tests;

public class NotationTests
{
    [Fact]
    public void Initial_ShouldWriteOpeningNotation_WhenStandardRules()
    {
        Assert.Equal("1fjajf1/2jrj2/7/7/7/2JRJ2/1FJAJF1 s 0 0:0", GameState.Initial().ToNotation());
    }

    [Fact]
    public void ToNotation_ShouldOmitWater_WhenWaterRuleIsOff()
    {
        Assert.Equal("1fjajf1/2jrj2/7/7/7/2JRJ2/1FJAJF1 s 0", GameState.Initial(RuleSet.V0_1).ToNotation());
    }

    [Fact]
    public void FromNotation_ShouldDefaultSidePlyAndWater_WhenOnlyBoardGiven()
    {
        var state = GameState.FromNotation(RuleSet.StandardSetup);

        Assert.Equal(Side.South, state.ToMove);
        Assert.Equal(0, state.Ply);
        Assert.Equal(0, state.Water(Side.North));
    }

    [Theory]
    [InlineData("1fjajf1/2jrj2/7/7/7/2JRJ2")]
    [InlineData("1fj1jf1/2jrj2/7/7/7/2JRJ2/1FJAJF1 s 0")]
    [InlineData("1fjajf1/2jrj2/7/7/7/2JRJ2/1FJAJF s 0")]
    [InlineData("1fjajf1/2jrj2/7/7/7/2JRJ2/1FJAJF1 x 0")]
    [InlineData("1fjajf1/2jrj2/7/7/7/2JRJ2/1FJAJF1 s 0 1")]
    [InlineData("1fjajf1/2jrj2/7/7/7/2JRJ2/1FJAJF1 s 0 0:0 extra")]
    [InlineData("1fjqjf1/2jrj2/7/7/7/2JRJ2/1FJAJF1 s 0")]
    public void FromNotation_ShouldThrowFormatException_WhenNotationIsBad(string text)
    {
        Assert.Throws<FormatException>(() => GameState.FromNotation(text));
    }

    [Theory]
    [InlineData("b1-b4", "b1", "b4", MoveKind.Step)]
    [InlineData("c3xd3", "c3", "d3", MoveKind.Capture)]
    [InlineData("d2*d4", "d2", "d4", MoveKind.Shot)]
    public void MoveParse_ShouldRoundTrip_WhenNotationIsValid(string text, string from, string to, MoveKind kind)
    {
        var move = Move.Parse(text);

        Assert.Equal(new Move(Square.Parse(from), Square.Parse(to), kind), move);
        Assert.Equal(text, move.Notation);
    }

    [Theory]
    [InlineData("h1-h2")]
    [InlineData("a1+a2")]
    [InlineData("a0-a1")]
    [InlineData("")]
    public void MoveTryParse_ShouldReturnFalse_WhenNotationIsBad(string text)
    {
        Assert.False(Move.TryParse(text, out _));
    }

    [Fact]
    public void Play_ShouldThrow_WhenMoveIsIllegal()
    {
        Assert.Throws<ArgumentException>(() => GameState.Initial().Play(Move.Parse("d1-d2")));
    }

    [Fact]
    public void EndReasonName_ShouldBeCamelCase_WhenFormatted()
    {
        Assert.Equal(
            ["amirCaptured", "qalaTaken", "noLegalMoves", "waterVictory", "plyLimitWater", "plyLimitWells", "plyLimitMaterial", "plyLimitDraw"],
            Enum.GetValues<EndReason>().Select(r => r.Name()));
    }

    [Fact]
    public void ForVersion_ShouldReturnStandard_WhenVersionIsCurrent()
    {
        Assert.Same(RuleSet.Standard, RuleSet.ForVersion("0.6"));
        Assert.Throws<ArgumentException>(() => RuleSet.ForVersion("9.9"));
    }
}
