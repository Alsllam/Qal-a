using System.Text.RegularExpressions;

namespace Qala.Game.Rules;

/// <summary>How a move affects the board.</summary>
public enum MoveKind
{
    /// <summary>Move to an empty square (<c>-</c>).</summary>
    Step = 0,

    /// <summary>Move onto an enemy piece and remove it (<c>x</c>).</summary>
    Capture = 1,

    /// <summary>Rami shot: remove the enemy on <see cref="Move.To"/>; the archer stays (<c>*</c>).</summary>
    Shot = 2,
}

/// <summary>A single move. Written as <c>b1-b4</c> (step), <c>c3xc4</c> (capture) or <c>d2*d4</c> (shot).</summary>
public readonly partial record struct Move(Square From, Square To, MoveKind Kind)
{
    public bool RemovesPiece => Kind != MoveKind.Step;

    public string Notation => $"{From.Name}{Symbol(Kind)}{To.Name}";

    public static char Symbol(MoveKind kind) => kind switch
    {
        MoveKind.Step => '-',
        MoveKind.Capture => 'x',
        MoveKind.Shot => '*',
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public static Move Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var match = MovePattern().Match(text);
        if (!match.Success)
        {
            throw new FormatException($"Bad move \"{text}\"");
        }

        var kind = match.Groups[2].Value switch
        {
            "-" => MoveKind.Step,
            "x" => MoveKind.Capture,
            _ => MoveKind.Shot,
        };
        return new Move(Square.Parse(match.Groups[1].Value), Square.Parse(match.Groups[3].Value), kind);
    }

    public static bool TryParse(string? text, out Move move)
    {
        move = default;
        if (text is null || !MovePattern().IsMatch(text))
        {
            return false;
        }

        move = Parse(text);
        return true;
    }

    public override string ToString() => Notation;

    [GeneratedRegex(@"^([a-g][1-7])([-x*])([a-g][1-7])$", RegexOptions.CultureInvariant)]
    private static partial Regex MovePattern();
}
