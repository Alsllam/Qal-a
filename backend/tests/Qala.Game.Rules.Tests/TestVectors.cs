using System.Text.Json;

namespace Qala.Game.Rules.Tests;

/// <summary>Loads <c>packages/game_core/test_vectors/rules_v{version}.json</c>, exported by the Dart reference engine.</summary>
public static class TestVectors
{
    private static readonly Lazy<VectorFile> Standard = new(() => Load(RuleSet.Standard.Version));

    public static VectorFile ForStandardRules => Standard.Value;

    public static VectorFile Load(string version)
    {
        var path = Path.Combine(FindRepoRoot(), "packages", "game_core", "test_vectors", $"rules_v{version}.json");
        using var stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<VectorFile>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException($"Empty vector file {path}");
    }

    /// <summary>Walks up from the test binaries until a directory containing <c>packages/game_core</c> is found.</summary>
    public static string FindRepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "packages", "game_core")))
            {
                return dir.FullName;
            }
        }

        throw new DirectoryNotFoundException($"No packages/game_core above {AppContext.BaseDirectory}");
    }

    /// <summary>Builds the C# rule set from the vector file's <c>rules</c> block.</summary>
    public static RuleSet ToRuleSet(VectorFile file) => new()
    {
        Version = file.RulesVersion,
        PlyLimit = file.Rules.PlyLimit,
        FarisRange = file.Rules.FarisRange,
        ShotDistance = file.Rules.ShotDistance,
        AmirIsSource = file.Rules.AmirIsSource,
        Wells = file.Rules.Wells.Select(Square.Parse).ToList(),
        SouthQala = Square.Parse(file.Rules.SouthQala),
        NorthQala = Square.Parse(file.Rules.NorthQala),
        Setup = file.Rules.Setup,
        WaterToWin = file.Rules.WaterToWin,
        WellsAreSources = file.Rules.WellsAreSources,
        WaterNeedsSupply = file.Rules.WaterNeedsSupply,
        NorthStartWater = file.Rules.NorthStartWater,
        AmirEarnsWater = file.Rules.AmirEarnsWater,
        DiagonalShots = file.Rules.DiagonalShots,
    };
}

public sealed record VectorFile(string RulesVersion, VectorRules Rules, IReadOnlyList<long> Perft, IReadOnlyList<VectorPosition> Positions);

public sealed record VectorRules(
    int PlyLimit,
    int FarisRange,
    int ShotDistance,
    bool AmirIsSource,
    IReadOnlyList<string> Wells,
    string SouthQala,
    string NorthQala,
    string Setup,
    int? WaterToWin,
    bool WellsAreSources,
    bool WaterNeedsSupply,
    int NorthStartWater,
    bool AmirEarnsWater,
    bool DiagonalShots);

public sealed record VectorPosition(
    string Position,
    IReadOnlyList<string> LegalMoves,
    IReadOnlyList<string> SuppliedSouth,
    IReadOnlyList<string> SuppliedNorth,
    string Move,
    string After,
    VectorOutcome? Outcome);

public sealed record VectorOutcome(string? Winner, string Reason);
