using System.Globalization;
using System.Text;

namespace Qala.Game.Rules;

/// <summary>
/// An immutable game position: the board, whose turn it is, the ply count, the water points and,
/// once the game is over, its <see cref="Outcome"/>. A faithful port of <c>game_state.dart</c>.
/// </summary>
/// <remarks>
/// Position notation: ranks 7 to 1 separated by <c>/</c>, upper case for South, lower case for North,
/// digits for runs of empty squares, then the side to move (<c>s</c>/<c>n</c>) and the ply count.
/// When the rule set uses water points a fourth field gives them as <c>south:north</c>.
/// </remarks>
public sealed class GameState
{
    private static readonly int[][] Neighbours = BuildNeighbours();

    private readonly Piece?[] _cells;
    private readonly int _southWater;
    private readonly int _northWater;
    private bool[]? _southSupply;
    private bool[]? _northSupply;
    private IReadOnlyList<Move>? _legalMoves;

    private GameState(RuleSet rules, Piece?[] cells, Side toMove, int ply, Outcome? outcome, int southWater, int northWater)
    {
        Rules = rules;
        _cells = cells;
        ToMove = toMove;
        Ply = ply;
        Outcome = outcome;
        _southWater = southWater;
        _northWater = northWater;
    }

    public RuleSet Rules { get; }

    /// <summary>The side whose turn it is.</summary>
    public Side ToMove { get; }

    /// <summary>Number of plies (half-moves) played so far.</summary>
    public int Ply { get; }

    /// <summary>Null while the game is in progress.</summary>
    public Outcome? Outcome { get; }

    public bool IsOver => Outcome is not null;

    /// <summary>The opening position for <paramref name="rules"/> (default <see cref="RuleSet.Standard"/>).</summary>
    public static GameState Initial(RuleSet? rules = null)
    {
        rules ??= RuleSet.Standard;
        return FromNotation(
            $"{rules.Setup} s 0 0:{rules.NorthStartWater.ToString(CultureInfo.InvariantCulture)}",
            rules);
    }

    /// <summary>
    /// Parses position notation. Side to move, ply count and water points are optional
    /// (defaults <c>s</c>, <c>0</c> and <c>0:0</c>). Each side must have exactly one Amir.
    /// </summary>
    public static GameState FromNotation(string text, RuleSet? rules = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        rules ??= RuleSet.Standard;
        var parts = text.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            parts = [string.Empty];
        }

        if (parts.Length > 4)
        {
            throw new FormatException($"Bad position \"{text}\"");
        }

        var ranks = parts[0].Split('/');
        if (ranks.Length != Square.BoardSize)
        {
            throw new FormatException($"Expected {Square.BoardSize} ranks in \"{text}\"");
        }

        var cells = new Piece?[Square.Count];
        for (var r = 0; r < Square.BoardSize; r++)
        {
            var rank = Square.BoardSize - 1 - r;
            var file = 0;
            foreach (var ch in ranks[r])
            {
                if (ch is >= '0' and <= '9')
                {
                    file += ch - '0';
                }
                else
                {
                    if (file >= Square.BoardSize)
                    {
                        break;
                    }

                    cells[new Square(file, rank).Index] = Piece.FromLetter(ch);
                    file++;
                }
            }

            if (file != Square.BoardSize)
            {
                throw new FormatException($"Rank {rank + 1} does not have {Square.BoardSize} squares in \"{text}\"");
            }
        }

        foreach (var side in new[] { Side.South, Side.North })
        {
            var amir = new Piece(PieceType.Amir, side);
            if (cells.Count(p => p == amir) != 1)
            {
                throw new FormatException($"{side.Name()} must have exactly one Amir");
            }
        }

        var toMove = parts.Length > 1 ? SideExtensions.FromCode(parts[1]) : Side.South;
        var ply = parts.Length > 2 ? ParseInt(parts[2]) : 0;
        int southWater = 0, northWater = 0;
        if (parts.Length > 3)
        {
            var water = parts[3].Split(':');
            var values = water.Select(ParseInt).ToArray();
            if (values.Length != 2)
            {
                throw new FormatException($"Bad water \"{parts[3]}\"");
            }

            southWater = values[0];
            northWater = values[1];
        }

        return new GameState(rules, cells, toMove, ply, null, southWater, northWater);
    }

    /// <summary>Water points of <paramref name="side"/> (always 0 unless <see cref="RuleSet.WaterToWin"/> is set).</summary>
    public int Water(Side side) => side == Side.South ? _southWater : _northWater;

    public Piece? At(Square square) => _cells[square.Index];

    /// <summary>All pieces with their squares, optionally for one side only.</summary>
    public IEnumerable<(Square Square, Piece Piece)> Pieces(Side? side = null)
    {
        for (var i = 0; i < Square.Count; i++)
        {
            if (_cells[i] is { } piece && (side is null || piece.Side == side))
            {
                yield return (Square.FromIndex(i), piece);
            }
        }
    }

    public int PieceCount(Side side) => _cells.Count(p => p?.Side == side);

    /// <summary>Square of <paramref name="side"/>'s Amir, or null if it has been captured.</summary>
    public Square? AmirSquare(Side side)
    {
        var index = Array.IndexOf(_cells, new Piece(PieceType.Amir, side));
        return index < 0 ? null : Square.FromIndex(index);
    }

    /// <summary>Number of Wells occupied by <paramref name="side"/>.</summary>
    public int WellsHeld(Side side) => Rules.Wells.Count(w => _cells[w.Index]?.Side == side);

    /// <summary>Wells that earn water points for <paramref name="side"/> this turn.</summary>
    private int WellsScoring(Side side) => Rules.Wells.Count(w =>
        _cells[w.Index] is { } piece
        && piece.Side == side
        && (Rules.AmirEarnsWater || piece.Type != PieceType.Amir)
        && (!Rules.WaterNeedsSupply || IsSupplied(w)));

    private bool[] SupplyOf(Side side) => side == Side.South
        ? _southSupply ??= ComputeSupply(Side.South)
        : _northSupply ??= ComputeSupply(Side.North);

    /// <summary>Whether the piece on <paramref name="square"/> is supplied. False for empty squares.</summary>
    public bool IsSupplied(Square square) =>
        _cells[square.Index] is { } piece && SupplyOf(piece.Side)[square.Index];

    /// <summary>Squares of all supplied pieces of <paramref name="side"/>, in board order.</summary>
    public IReadOnlyList<Square> SuppliedSquares(Side side)
    {
        var mask = SupplyOf(side);
        var result = new List<Square>();
        for (var i = 0; i < Square.Count; i++)
        {
            if (mask[i])
            {
                result.Add(Square.FromIndex(i));
            }
        }

        return result;
    }

    /// <summary>Flood fill from the water sources through touching friendly pieces.</summary>
    private bool[] ComputeSupply(Side side)
    {
        var mask = new bool[Square.Count];
        var queue = new List<int>(Square.Count);

        void Seed(int i)
        {
            if (!mask[i] && _cells[i]?.Side == side)
            {
                mask[i] = true;
                queue.Add(i);
            }
        }

        var qala = Rules.QalaOf(side).Index;
        if (_cells[qala]?.Side != side.Opponent())
        {
            Seed(qala);
            foreach (var n in Neighbours[qala])
            {
                Seed(n);
            }
        }

        if (Rules.WellsAreSources)
        {
            foreach (var well in Rules.Wells)
            {
                Seed(well.Index);
            }
        }

        if (Rules.AmirIsSource)
        {
            var amir = Array.IndexOf(_cells, new Piece(PieceType.Amir, side));
            if (amir >= 0)
            {
                Seed(amir);
            }
        }

        for (var head = 0; head < queue.Count; head++)
        {
            foreach (var n in Neighbours[queue[head]])
            {
                Seed(n);
            }
        }

        return mask;
    }

    /// <summary>Every legal move for <see cref="ToMove"/>. Empty once the game is over.</summary>
    public IReadOnlyList<Move> LegalMoves => _legalMoves ??= IsOver ? [] : GenerateMoves().AsReadOnly();

    /// <summary>Legal moves of the piece on <paramref name="from"/>.</summary>
    public IReadOnlyList<Move> LegalMovesFrom(Square from) => LegalMoves.Where(m => m.From == from).ToList();

    public bool IsLegal(Move move) => LegalMoves.Contains(move);

    private List<Move> GenerateMoves()
    {
        var moves = new List<Move>();
        var supply = SupplyOf(ToMove);
        for (var i = 0; i < Square.Count; i++)
        {
            if (_cells[i] is not { } piece || piece.Side != ToMove)
            {
                continue;
            }

            var from = Square.FromIndex(i);
            var canCapture = supply[i];
            switch (piece.Type)
            {
                case PieceType.Amir:
                    Slide(from, Directions.All, 1, canCapture, moves);
                    break;
                case PieceType.Jundi:
                    Slide(from, Directions.Orthogonal, 1, canCapture, moves);
                    break;
                case PieceType.Faris:
                    Slide(from, Directions.Orthogonal, Rules.FarisRange, canCapture, moves);
                    break;
                case PieceType.Rami:
                    foreach (var (df, dr) in Directions.Diagonal)
                    {
                        if (from.Offset(df, dr) is { } to && _cells[to.Index] is null)
                        {
                            moves.Add(new Move(from, to, MoveKind.Step));
                        }
                    }

                    if (canCapture)
                    {
                        AddShots(from, moves);
                    }

                    break;
            }
        }

        return moves;
    }

    private void Slide(Square from, IReadOnlyList<(int Df, int Dr)> directions, int range, bool canCapture, List<Move> moves)
    {
        foreach (var (df, dr) in directions)
        {
            for (var d = 1; d <= range; d++)
            {
                if (from.Offset(df * d, dr * d) is not { } to)
                {
                    break;
                }

                if (_cells[to.Index] is not { } target)
                {
                    moves.Add(new Move(from, to, MoveKind.Step));
                    continue;
                }

                if (target.Side != ToMove && canCapture)
                {
                    moves.Add(new Move(from, to, MoveKind.Capture));
                }

                break;
            }
        }
    }

    private void AddShots(Square from, List<Move> moves)
    {
        var distance = Rules.ShotDistance;
        var directions = Rules.DiagonalShots ? Directions.All : Directions.Orthogonal;
        foreach (var (df, dr) in directions)
        {
            if (from.Offset(df * distance, dr * distance) is not { } target
                || _cells[target.Index]?.Side != ToMove.Opponent())
            {
                continue;
            }

            var clear = true;
            for (var d = 1; d < distance && clear; d++)
            {
                clear = _cells[from.Offset(df * d, dr * d)!.Value.Index] is null;
            }

            if (clear)
            {
                moves.Add(new Move(from, target, MoveKind.Shot));
            }
        }
    }

    /// <summary>Plays a legal <paramref name="move"/> and returns the new position.</summary>
    /// <exception cref="InvalidOperationException">The game is over.</exception>
    /// <exception cref="ArgumentException">The move is illegal.</exception>
    public GameState Play(Move move)
    {
        if (IsOver)
        {
            throw new InvalidOperationException($"Game is over: {Outcome}");
        }

        if (!IsLegal(move))
        {
            throw new ArgumentException($"Illegal move {move.Notation}", nameof(move));
        }

        return PlayUnchecked(move);
    }

    /// <summary>Plays <paramref name="move"/> without checking legality. For search code only.</summary>
    public GameState PlayUnchecked(Move move)
    {
        var cells = (Piece?[])_cells.Clone();
        var mover = ToMove;
        var removed = move.RemovesPiece ? cells[move.To.Index] : null;
        switch (move.Kind)
        {
            case MoveKind.Step:
            case MoveKind.Capture:
                cells[move.To.Index] = cells[move.From.Index];
                cells[move.From.Index] = null;
                break;
            case MoveKind.Shot:
                cells[move.To.Index] = null;
                break;
        }

        var southWater = _southWater;
        var northWater = _northWater;
        GameState Build(Outcome? outcome) =>
            new(Rules, cells, mover.Opponent(), Ply + 1, outcome, southWater, northWater);

        if (Rules.WaterToWin is not null)
        {
            // The side about to move draws water from the Wells it still holds.
            var gained = Build(null).WellsScoring(mover.Opponent());
            if (mover == Side.South)
            {
                northWater += gained;
            }
            else
            {
                southWater += gained;
            }
        }

        var next = Build(null);
        var outcome = next.Judge(mover, removed);
        return outcome is null ? next : Build(outcome);
    }

    /// <summary>Decides whether the move just made by <paramref name="mover"/> ended the game.</summary>
    private Outcome? Judge(Side mover, Piece? removed)
    {
        if (removed?.Type == PieceType.Amir)
        {
            return new Outcome(mover, EndReason.AmirCaptured);
        }

        var enemyQala = Rules.QalaOf(mover.Opponent());
        if (At(enemyQala)?.Side == mover && IsSupplied(enemyQala))
        {
            return new Outcome(mover, EndReason.QalaTaken);
        }

        if (Rules.WaterToWin is { } waterToWin && Water(ToMove) >= waterToWin)
        {
            return new Outcome(ToMove, EndReason.WaterVictory);
        }

        if (Ply >= Rules.PlyLimit)
        {
            return PlyLimitOutcome();
        }

        if (LegalMoves.Count == 0)
        {
            return new Outcome(mover, EndReason.NoLegalMoves);
        }

        return null;
    }

    private Outcome PlyLimitOutcome()
    {
        var waterLead = Water(Side.South) - Water(Side.North);
        if (waterLead != 0)
        {
            return new Outcome(waterLead > 0 ? Side.South : Side.North, EndReason.PlyLimitWater);
        }

        var wells = WellsHeld(Side.South) - WellsHeld(Side.North);
        if (wells != 0)
        {
            return new Outcome(wells > 0 ? Side.South : Side.North, EndReason.PlyLimitWells);
        }

        var material = PieceCount(Side.South) - PieceCount(Side.North);
        if (material != 0)
        {
            return new Outcome(material > 0 ? Side.South : Side.North, EndReason.PlyLimitMaterial);
        }

        return new Outcome(null, EndReason.PlyLimitDraw);
    }

    /// <summary>Position notation, e.g. <c>1fjajf1/2jrj2/7/7/7/2JRJ2/1FJAJF1 s 0 0:0</c>.</summary>
    public string ToNotation()
    {
        var builder = new StringBuilder(48);
        for (var rank = Square.BoardSize - 1; rank >= 0; rank--)
        {
            var empty = 0;
            for (var file = 0; file < Square.BoardSize; file++)
            {
                if (_cells[new Square(file, rank).Index] is not { } piece)
                {
                    empty++;
                    continue;
                }

                if (empty > 0)
                {
                    builder.Append(empty);
                }

                empty = 0;
                builder.Append(piece.Letter);
            }

            if (empty > 0)
            {
                builder.Append(empty);
            }

            if (rank > 0)
            {
                builder.Append('/');
            }
        }

        builder.Append(' ').Append(ToMove.Code()).Append(' ').Append(Ply.ToString(CultureInfo.InvariantCulture));
        if (Rules.WaterToWin is not null)
        {
            builder.Append(' ')
                .Append(_southWater.ToString(CultureInfo.InvariantCulture))
                .Append(':')
                .Append(_northWater.ToString(CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    /// <summary>Text diagram. Empty Qal'a squares show <c>Q</c>, empty Wells <c>W</c>, unsupplied pieces <c>'</c>.</summary>
    public string ToAscii()
    {
        var builder = new StringBuilder("    a  b  c  d  e  f  g\n");
        for (var rank = Square.BoardSize - 1; rank >= 0; rank--)
        {
            builder.Append(rank + 1).Append(" |");
            for (var file = 0; file < Square.BoardSize; file++)
            {
                var square = new Square(file, rank);
                string cell;
                if (At(square) is { } piece)
                {
                    cell = $"{piece.Letter}{(IsSupplied(square) ? ' ' : '\'')}";
                }
                else if (square == Rules.SouthQala || square == Rules.NorthQala)
                {
                    cell = "Q ";
                }
                else if (Rules.Wells.Contains(square))
                {
                    cell = "W ";
                }
                else
                {
                    cell = ". ";
                }

                builder.Append(' ').Append(cell);
            }

            builder.Append('\n');
        }

        return builder.ToString();
    }

    /// <summary>Counts leaf positions to <paramref name="depth"/> (finished games count as one leaf).</summary>
    public static long Perft(GameState state, int depth)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (depth == 0 || state.IsOver)
        {
            return 1;
        }

        long total = 0;
        foreach (var move in state.LegalMoves)
        {
            total += Perft(state.PlayUnchecked(move), depth - 1);
        }

        return total;
    }

    public override bool Equals(object? obj) =>
        obj is GameState other && other.ToMove == ToMove && other.Ply == Ply && other.ToNotation() == ToNotation();

    public override int GetHashCode() => ToNotation().GetHashCode(StringComparison.Ordinal);

    public override string ToString() => ToNotation();

    private static int ParseInt(string text) =>
        int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value)
            ? value
            : throw new FormatException($"Bad number \"{text}\"");

    private static int[][] BuildNeighbours()
    {
        var result = new int[Square.Count][];
        for (var i = 0; i < Square.Count; i++)
        {
            var square = Square.FromIndex(i);
            result[i] = Directions.All
                .Select(d => square.Offset(d.Df, d.Dr))
                .Where(n => n is not null)
                .Select(n => n!.Value.Index)
                .ToArray();
        }

        return result;
    }
}
