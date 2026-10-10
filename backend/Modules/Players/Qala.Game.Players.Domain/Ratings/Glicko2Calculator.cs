namespace Qala.Game.Players.Domain.Ratings;

/// <summary>A Glicko-2 rating on the Glicko scale (1500 ± 350 for a new player).</summary>
public sealed record Glicko2Rating(double Rating, double Deviation, double Volatility)
{
    public static Glicko2Rating Initial { get; } = new(1500, 350, 0.06);
}

/// <summary>One game in a rating period: the opponent's rating before the game and the score (1, 0.5 or 0).</summary>
public sealed record Glicko2Game(Glicko2Rating Opponent, double Score);

/// <summary>
/// Glicko-2 (Mark Glickman, "Example of the Glicko-2 system", 2013). Each finished match is treated as its own rating
/// period, so both players are updated from their pre-game ratings.
/// </summary>
public static class Glicko2Calculator
{
    /// <summary>Glicko → Glicko-2 scale factor.</summary>
    public const double Scale = 173.7178;

    /// <summary>System constant: how much volatility may change. The paper's example uses 0.5.</summary>
    public const double DefaultTau = 0.5;

    public const double MaxDeviation = 350;

    private const double ConvergenceTolerance = 0.000001;

    /// <summary>Returns <paramref name="player"/>'s rating after <paramref name="games"/>.</summary>
    public static Glicko2Rating Update(Glicko2Rating player, IReadOnlyCollection<Glicko2Game> games, double tau = DefaultTau)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(games);
        var mu = (player.Rating - 1500) / Scale;
        var phi = player.Deviation / Scale;
        var sigma = player.Volatility;

        if (games.Count == 0)
        {
            // Step 6 only: the deviation grows while the player does not play.
            var idle = Math.Sqrt(phi * phi + sigma * sigma);
            return player with { Deviation = Math.Min(MaxDeviation, idle * Scale) };
        }

        // Steps 3 and 4: estimated variance and improvement.
        double vInverse = 0, deltaSum = 0;
        foreach (var game in games)
        {
            var muJ = (game.Opponent.Rating - 1500) / Scale;
            var phiJ = game.Opponent.Deviation / Scale;
            var g = G(phiJ);
            var e = E(mu, muJ, phiJ);
            vInverse += g * g * e * (1 - e);
            deltaSum += g * (game.Score - e);
        }

        var v = 1 / vInverse;
        var delta = v * deltaSum;

        // Step 5: new volatility (Illinois algorithm).
        var newSigma = NewVolatility(sigma, phi, v, delta, tau);

        // Steps 6 and 7: new deviation and rating.
        var phiStar = Math.Sqrt(phi * phi + newSigma * newSigma);
        var newPhi = 1 / Math.Sqrt(1 / (phiStar * phiStar) + 1 / v);
        var newMu = mu + newPhi * newPhi * deltaSum;

        // Step 8: back to the Glicko scale.
        return new Glicko2Rating(Scale * newMu + 1500, Math.Min(MaxDeviation, Scale * newPhi), newSigma);
    }

    private static double G(double phi) => 1 / Math.Sqrt(1 + 3 * phi * phi / (Math.PI * Math.PI));

    private static double E(double mu, double muJ, double phiJ) => 1 / (1 + Math.Exp(-G(phiJ) * (mu - muJ)));

    private static double NewVolatility(double sigma, double phi, double v, double delta, double tau)
    {
        var a = Math.Log(sigma * sigma);
        double F(double x)
        {
            var ex = Math.Exp(x);
            var denominator = phi * phi + v + ex;
            return ex * (delta * delta - phi * phi - v - ex) / (2 * denominator * denominator) - (x - a) / (tau * tau);
        }

        var lower = a;
        double upper;
        if (delta * delta > phi * phi + v)
        {
            upper = Math.Log(delta * delta - phi * phi - v);
        }
        else
        {
            var k = 1;
            while (F(a - k * tau) < 0)
            {
                k++;
            }

            upper = a - k * tau;
        }

        var fLower = F(lower);
        var fUpper = F(upper);
        while (Math.Abs(upper - lower) > ConvergenceTolerance)
        {
            var c = lower + (lower - upper) * fLower / (fUpper - fLower);
            var fC = F(c);
            if (fC * fUpper <= 0)
            {
                lower = upper;
                fLower = fUpper;
            }
            else
            {
                fLower /= 2;
            }

            upper = c;
            fUpper = fC;
        }

        return Math.Exp(lower / 2);
    }
}
