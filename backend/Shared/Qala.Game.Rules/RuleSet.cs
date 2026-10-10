namespace Qala.Game.Rules;

/// <summary>
/// Every tunable rule parameter. Mirrors <c>rule_set.dart</c>; the defaults are the Dart defaults
/// (rules v0.1 behaviour), and <see cref="Standard"/> is the current rules (v0.6).
/// </summary>
public sealed record RuleSet
{
    /// <summary>Board part of the standard opening position (rank 7 first).</summary>
    public const string StandardSetup = "1fjajf1/2jrj2/7/7/7/2JRJ2/1FJAJF1";

    /// <summary>The current rules, as written in docs/rules.md (version 0.6).</summary>
    public static readonly RuleSet Standard = new()
    {
        Version = "0.6",
        WaterToWin = 10,
        WellsAreSources = false,
        WaterNeedsSupply = true,
        AmirEarnsWater = false,
        DiagonalShots = false,
    };

    /// <summary>The first ruleset, kept for reference and old replays.</summary>
    public static readonly RuleSet V0_1 = new() { Version = "0.1" };

    /// <summary>Rules version label, recorded with every match.</summary>
    public required string Version { get; init; }

    /// <summary>The game ends after this many plies if nobody has won.</summary>
    public int PlyLimit { get; init; } = 60;

    /// <summary>Maximum slide distance of the Faris.</summary>
    public int FarisRange { get; init; } = 3;

    /// <summary>Exact distance of a Rami shot.</summary>
    public int ShotDistance { get; init; } = 2;

    /// <summary>Whether the Amir is a water source.</summary>
    public bool AmirIsSource { get; init; } = true;

    /// <summary>Well squares.</summary>
    public IReadOnlyList<Square> Wells { get; init; } = [new Square(2, 3), new Square(4, 3)];

    public Square SouthQala { get; init; } = new(3, 0);

    public Square NorthQala { get; init; } = new(3, 6);

    /// <summary>Board part of the opening position in position notation.</summary>
    public string Setup { get; init; } = StandardSetup;

    /// <summary>Water points needed to win, or null if the water rule is off.</summary>
    public int? WaterToWin { get; init; }

    /// <summary>Whether a piece standing on a Well is a water source for supply.</summary>
    public bool WellsAreSources { get; init; } = true;

    /// <summary>Whether a Well earns water points only while the piece on it is supplied.</summary>
    public bool WaterNeedsSupply { get; init; }

    /// <summary>Water points North starts with (compensation for moving second).</summary>
    public int NorthStartWater { get; init; }

    /// <summary>Whether an Amir standing on a Well earns water points.</summary>
    public bool AmirEarnsWater { get; init; } = true;

    /// <summary>Whether the Rami may shoot diagonally (otherwise orthogonally only).</summary>
    public bool DiagonalShots { get; init; } = true;

    public Square QalaOf(Side side) => side == Side.South ? SouthQala : NorthQala;

    /// <summary>The published rule sets by version. Every match records its version so it can be replayed.</summary>
    public static IReadOnlyDictionary<string, RuleSet> Published { get; } = new Dictionary<string, RuleSet>(StringComparer.Ordinal)
    {
        [V0_1.Version] = V0_1,
        [Standard.Version] = Standard,
    };

    /// <summary>Returns the rule set for <paramref name="version"/>, or throws if it is unknown.</summary>
    public static RuleSet ForVersion(string version) =>
        Published.TryGetValue(version, out var rules)
            ? rules
            : throw new ArgumentException($"Unknown rules version \"{version}\"", nameof(version));

    public bool Equals(RuleSet? other) =>
        other is not null
        && Version == other.Version
        && PlyLimit == other.PlyLimit
        && FarisRange == other.FarisRange
        && ShotDistance == other.ShotDistance
        && AmirIsSource == other.AmirIsSource
        && Wells.SequenceEqual(other.Wells)
        && SouthQala == other.SouthQala
        && NorthQala == other.NorthQala
        && Setup == other.Setup
        && WaterToWin == other.WaterToWin
        && WellsAreSources == other.WellsAreSources
        && WaterNeedsSupply == other.WaterNeedsSupply
        && NorthStartWater == other.NorthStartWater
        && AmirEarnsWater == other.AmirEarnsWater
        && DiagonalShots == other.DiagonalShots;

    public override int GetHashCode() => HashCode.Combine(Version, PlyLimit, FarisRange, ShotDistance, Setup, WaterToWin);
}
