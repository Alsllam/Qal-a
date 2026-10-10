using Qala.Game.Players.Domain.Ratings;

namespace Qala.Game.Players.Tests.Domain;

public class Glicko2CalculatorTests
{
    [Fact]
    public void Update_ShouldMatchGlickmansWorkedExample_WhenPlayerHasThreeGames()
    {
        // Arrange: the example from Glickman's "Example of the Glicko-2 system" (tau = 0.5).
        var player = new Glicko2Rating(1500, 200, 0.06);
        Glicko2Game[] games =
        [
            new(new Glicko2Rating(1400, 30, 0.06), 1),
            new(new Glicko2Rating(1550, 100, 0.06), 0),
            new(new Glicko2Rating(1700, 300, 0.06), 0),
        ];

        // Act
        var updated = Glicko2Calculator.Update(player, games);

        // Assert
        // The paper rounds intermediate values (mu' = -0.2069); unrounded arithmetic gives 1464.0507.
        Assert.Equal(1464.06, updated.Rating, tolerance: 0.02);
        Assert.Equal(151.52, updated.Deviation, 2);
        Assert.Equal(0.05999, updated.Volatility, tolerance: 0.00001);
    }

    [Fact]
    public void Update_ShouldOnlyWidenDeviation_WhenPlayerHasNoGames()
    {
        var player = new Glicko2Rating(1500, 200, 0.06);

        var updated = Glicko2Calculator.Update(player, []);

        Assert.Equal(1500, updated.Rating);
        Assert.True(updated.Deviation > 200);
        Assert.Equal(0.06, updated.Volatility);
    }

    [Fact]
    public void Update_ShouldBeSymmetric_WhenEqualPlayersMeet()
    {
        var a = Glicko2Rating.Initial;

        var winner = Glicko2Calculator.Update(a, [new Glicko2Game(a, 1)]);
        var loser = Glicko2Calculator.Update(a, [new Glicko2Game(a, 0)]);
        var drawn = Glicko2Calculator.Update(a, [new Glicko2Game(a, 0.5)]);

        Assert.Equal(winner.Rating - 1500, 1500 - loser.Rating, 6);
        Assert.Equal(1500, drawn.Rating, 6);
        Assert.True(winner.Deviation < 350);
    }
}
