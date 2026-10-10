namespace Qala.Game.Rules;

/// <summary>The two players. South moves first.</summary>
public enum Side
{
    South = 0,
    North = 1,
}

public static class SideExtensions
{
    public static Side Opponent(this Side side) => side == Side.South ? Side.North : Side.South;

    /// <summary>Single-letter code used in position notation (<c>s</c> / <c>n</c>).</summary>
    public static string Code(this Side side) => side == Side.South ? "s" : "n";

    /// <summary>Lower-case name as used by the Dart engine and the API (<c>south</c> / <c>north</c>).</summary>
    public static string Name(this Side side) => side == Side.South ? "south" : "north";

    public static Side FromCode(string code) => code switch
    {
        "s" => Side.South,
        "n" => Side.North,
        _ => throw new FormatException($"Unknown side \"{code}\""),
    };

    /// <summary>Parses <c>south</c> / <c>north</c>.</summary>
    public static Side FromName(string name) => name switch
    {
        "south" => Side.South,
        "north" => Side.North,
        _ => throw new FormatException($"Unknown side \"{name}\""),
    };
}
