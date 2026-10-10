namespace Qala.Game.Rules;

/// <summary>The four piece types.</summary>
public enum PieceType
{
    /// <summary>Leader. One step in any direction. Losing it loses the game.</summary>
    Amir = 0,

    /// <summary>Soldier. One step orthogonally.</summary>
    Jundi = 1,

    /// <summary>Horseman. Slides up to <see cref="RuleSet.FarisRange"/> squares orthogonally.</summary>
    Faris = 2,

    /// <summary>Archer. Steps one square diagonally; shoots instead of capturing by moving.</summary>
    Rami = 3,
}

/// <summary>A piece: a type and an owner.</summary>
public readonly record struct Piece(PieceType Type, Side Side)
{
    /// <summary>Notation letter: upper case for South, lower case for North.</summary>
    public char Letter
    {
        get
        {
            var upper = TypeLetter(Type);
            return Side == Side.South ? upper : char.ToLowerInvariant(upper);
        }
    }

    public static char TypeLetter(PieceType type) => type switch
    {
        PieceType.Amir => 'A',
        PieceType.Jundi => 'J',
        PieceType.Faris => 'F',
        PieceType.Rami => 'R',
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    /// <summary>Parses a notation letter: upper case is South, lower case is North.</summary>
    public static Piece FromLetter(char letter)
    {
        var side = letter == char.ToUpperInvariant(letter) ? Side.South : Side.North;
        var type = char.ToUpperInvariant(letter) switch
        {
            'A' => PieceType.Amir,
            'J' => PieceType.Jundi,
            'F' => PieceType.Faris,
            'R' => PieceType.Rami,
            _ => throw new FormatException($"Unknown piece \"{letter}\""),
        };
        return new Piece(type, side);
    }

    public override string ToString() => Letter.ToString();
}
