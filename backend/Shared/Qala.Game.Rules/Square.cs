namespace Qala.Game.Rules;

/// <summary>
/// A square on the 7×7 board. Files <c>a–g</c> map to 0–6, ranks <c>1–7</c> to 0–6.
/// Rank 1 is South's home rank. Mirrors <c>square.dart</c>.
/// </summary>
public readonly record struct Square : IComparable<Square>
{
    /// <summary>Board width and height.</summary>
    public const int BoardSize = 7;

    /// <summary>Number of squares on the board.</summary>
    public const int Count = BoardSize * BoardSize;

    public Square(int file, int rank)
    {
        if (!IsOnBoard(file, rank))
        {
            throw new ArgumentOutOfRangeException(nameof(file), $"Square ({file}, {rank}) is off the board");
        }

        File = file;
        Rank = rank;
    }

    public int File { get; }

    public int Rank { get; }

    public int Index => Rank * BoardSize + File;

    /// <summary>Algebraic name such as <c>d4</c>.</summary>
    public string Name => string.Create(2, (File, Rank), static (span, s) =>
    {
        span[0] = (char)('a' + s.File);
        span[1] = (char)('1' + s.Rank);
    });

    public static Square FromIndex(int index)
    {
        if (index is < 0 or >= Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return new Square(index % BoardSize, index / BoardSize);
    }

    /// <summary>Parses algebraic names such as <c>d4</c>.</summary>
    public static Square Parse(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (name.Length != 2)
        {
            throw new FormatException($"Bad square \"{name}\"");
        }

        var file = name[0] - 'a';
        var rank = name[1] - '1';
        if (!IsOnBoard(file, rank))
        {
            throw new FormatException($"Bad square \"{name}\"");
        }

        return new Square(file, rank);
    }

    /// <summary>The square <paramref name="df"/> files and <paramref name="dr"/> ranks away, or null if off the board.</summary>
    public Square? Offset(int df, int dr)
    {
        var f = File + df;
        var r = Rank + dr;
        return IsOnBoard(f, r) ? new Square(f, r) : null;
    }

    public static bool IsOnBoard(int file, int rank) =>
        file is >= 0 and < BoardSize && rank is >= 0 and < BoardSize;

    public int CompareTo(Square other) => Index.CompareTo(other.Index);

    public static bool operator <(Square left, Square right) => left.CompareTo(right) < 0;

    public static bool operator >(Square left, Square right) => left.CompareTo(right) > 0;

    public static bool operator <=(Square left, Square right) => left.CompareTo(right) <= 0;

    public static bool operator >=(Square left, Square right) => left.CompareTo(right) >= 0;

    public override string ToString() => Name;
}

/// <summary>Direction deltas as (file, rank), in the same order as the Dart engine.</summary>
public static class Directions
{
    /// <summary>The four orthogonal directions.</summary>
    public static readonly IReadOnlyList<(int Df, int Dr)> Orthogonal = [(0, 1), (1, 0), (0, -1), (-1, 0)];

    /// <summary>The four diagonal directions.</summary>
    public static readonly IReadOnlyList<(int Df, int Dr)> Diagonal = [(1, 1), (1, -1), (-1, -1), (-1, 1)];

    /// <summary>All eight directions (orthogonal first).</summary>
    public static readonly IReadOnlyList<(int Df, int Dr)> All = [.. Orthogonal, .. Diagonal];
}
