using Qala.Framework.Domain.Entities;
using Qala.Framework.Domain.Events;
using Qala.Game.Matches.Domain.Constants;
using Qala.Game.Matches.Domain.Enums;
using Qala.Game.Matches.Domain.ValueObjects;
using Qala.Game.Rules;

namespace Qala.Game.Matches.Domain.Entities;

/// <summary>A participant snapshot taken when the match starts. <paramref name="PlayerId"/> is the auth user id.</summary>
public sealed record PlayerRef(Guid PlayerId, string DisplayName, double? Rating);

/// <summary>The result of <see cref="Match.ApplyMove"/>.</summary>
public sealed record MoveResult(MoveStatus Status, string? ErrorKey = null)
{
    public static MoveResult Accepted { get; } = new(MoveStatus.Accepted);

    public static MoveResult Duplicate { get; } = new(MoveStatus.Duplicate);

    public static MoveResult Rejected(string errorKey) => new(MoveStatus.Rejected, errorKey);
}

/// <summary>
/// An online match. The server is authoritative: every move is validated with <see cref="GameState"/> for the
/// match's <see cref="RulesVersion"/>, and the clocks run on the server.
/// </summary>
public class Match : FullAuditedEntity<Guid>, IHasConcurrencyStamp
{
    private readonly List<MatchMove> _moves = [];

    protected Match()
    {
    }

    private Match(Guid id, RuleSet rules, TimeControl timeControl)
        : base(id)
    {
        RulesVersion = rules.Version;
        Position = GameState.Initial(rules).ToNotation();
        InitialMs = timeControl.InitialMs;
        IncrementMs = timeControl.IncrementMs;
        SouthClockMs = timeControl.InitialMs;
        NorthClockMs = timeControl.InitialMs;
    }

    public string RulesVersion { get; private set; } = string.Empty;

    public MatchStatus Status { get; private set; }

    /// <summary>Six-character code a friend types to accept; cleared once the match starts.</summary>
    public string? ChallengeCode { get; private set; }

    public Guid CreatorPlayerId { get; private set; }

    public string CreatorDisplayName { get; private set; } = string.Empty;

    public double? CreatorRating { get; private set; }

    public Guid? SouthPlayerId { get; private set; }

    public string? SouthDisplayName { get; private set; }

    public double? SouthRating { get; private set; }

    public Guid? NorthPlayerId { get; private set; }

    public string? NorthDisplayName { get; private set; }

    public double? NorthRating { get; private set; }

    /// <summary>Current position in rules notation (water points included).</summary>
    public string Position { get; private set; } = string.Empty;

    /// <summary>Plies played so far; a client's <c>MakeMove</c> must send this value.</summary>
    public int Ply { get; private set; }

    public long InitialMs { get; private set; }

    public long IncrementMs { get; private set; }

    /// <summary>South's remaining time when its current turn started (or after its last move).</summary>
    public long SouthClockMs { get; private set; }

    public long NorthClockMs { get; private set; }

    /// <summary>When the side to move started thinking. Null unless active.</summary>
    public DateTime? TurnStartedAt { get; private set; }

    /// <summary>When the side to move runs out of time. Indexed so the clock sweeper can find expired games.</summary>
    public DateTime? FlagFallsAt { get; private set; }

    public DateTime? StartedAt { get; private set; }

    public DateTime? FinishedAt { get; private set; }

    /// <summary>Null for a draw or an unfinished match.</summary>
    public Side? Winner { get; private set; }

    public MatchEndReason? EndReason { get; private set; }

    public string ConcurrencyStamp { get; set; } = string.Empty;

    public IReadOnlyList<MatchMove> Moves => _moves;

    public Side SideToMove => Ply % 2 == 0 ? Side.South : Side.North;

    public TimeControl TimeControl => new((int)(InitialMs / 1000), (int)(IncrementMs / 1000));

    /// <summary>A friend challenge waiting for its code to be accepted.</summary>
    public static Match CreateChallenge(Guid id, PlayerRef creator, string code, TimeControl timeControl, RuleSet rules)
    {
        ArgumentNullException.ThrowIfNull(creator);
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(timeControl);
        if (code.Length != MatchConsts.ChallengeCodeLength)
        {
            throw new ArgumentException("Bad challenge code", nameof(code));
        }

        return new Match(id, rules, timeControl)
        {
            Status = MatchStatus.Waiting,
            ChallengeCode = code,
            CreatorPlayerId = creator.PlayerId,
            CreatorDisplayName = creator.DisplayName,
            CreatorRating = creator.Rating,
        };
    }

    /// <summary>A match between two players paired by matchmaking; it starts immediately.</summary>
    public static Match CreatePaired(Guid id, PlayerRef south, PlayerRef north, TimeControl timeControl, RuleSet rules, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(south);
        ArgumentNullException.ThrowIfNull(north);
        var match = new Match(id, rules, timeControl)
        {
            CreatorPlayerId = south.PlayerId,
            CreatorDisplayName = south.DisplayName,
            CreatorRating = south.Rating,
        };
        match.Start(south, north, now);
        return match;
    }

    /// <summary>A second player accepts the challenge. <paramref name="creatorPlaysSouth"/> is drawn by the caller.</summary>
    public void AcceptChallenge(PlayerRef opponent, bool creatorPlaysSouth, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(opponent);
        if (Status != MatchStatus.Waiting)
        {
            throw new InvalidOperationException(MatchErrors.NotActive);
        }

        if (opponent.PlayerId == CreatorPlayerId)
        {
            throw new InvalidOperationException(MatchErrors.OwnChallenge);
        }

        var creator = new PlayerRef(CreatorPlayerId, CreatorDisplayName, CreatorRating);
        Start(creatorPlaysSouth ? creator : opponent, creatorPlaysSouth ? opponent : creator, now);
    }

    /// <summary>The side <paramref name="playerId"/> plays, or null for a spectator.</summary>
    public Side? SideOf(Guid playerId) =>
        playerId == SouthPlayerId ? Side.South : playerId == NorthPlayerId ? Side.North : null;

    public bool IsParticipant(Guid playerId) => SideOf(playerId) is not null || (Status == MatchStatus.Waiting && playerId == CreatorPlayerId);

    /// <summary>Remaining time of <paramref name="side"/> at <paramref name="now"/> (the side to move's clock is running).</summary>
    public long RemainingMs(Side side, DateTime now)
    {
        var stored = side == Side.South ? SouthClockMs : NorthClockMs;
        if (Status != MatchStatus.Active || side != SideToMove || TurnStartedAt is not { } started)
        {
            return stored;
        }

        return Math.Max(0, stored - (long)(now - started).TotalMilliseconds);
    }

    /// <summary>
    /// Plays <paramref name="moveText"/> for <paramref name="playerId"/>. <paramref name="ply"/> must equal
    /// <see cref="Ply"/>; resending an already played move at its old ply is a harmless <see cref="MoveStatus.Duplicate"/>.
    /// A move that arrives after the mover's flag fell ends the game on time instead.
    /// </summary>
    public MoveResult ApplyMove(Guid playerId, string moveText, int ply, DateTime now)
    {
        var side = SideOf(playerId);
        if (side is null)
        {
            return MoveResult.Rejected(MatchErrors.NotParticipant);
        }

        if (ply >= 0 && ply < Ply && ply < _moves.Count && OrderedMoves().ElementAt(ply).Notation == moveText)
        {
            return MoveResult.Duplicate;
        }

        if (Status != MatchStatus.Active)
        {
            return MoveResult.Rejected(MatchErrors.NotActive);
        }

        if (ply != Ply)
        {
            return MoveResult.Rejected(MatchErrors.StalePly);
        }

        if (side != SideToMove)
        {
            return MoveResult.Rejected(MatchErrors.NotYourTurn);
        }

        var remaining = RemainingMs(side.Value, now);
        if (remaining <= 0)
        {
            FinishOnTime(now);
            return MoveResult.Rejected(MatchErrors.Timeout);
        }

        var state = CurrentState();
        if (!Rules.Move.TryParse(moveText, out var move) || !state.IsLegal(move))
        {
            return MoveResult.Rejected(MatchErrors.IllegalMove);
        }

        var next = state.Play(move);
        var clockAfter = remaining + IncrementMs;
        if (side == Side.South)
        {
            SouthClockMs = clockAfter;
        }
        else
        {
            NorthClockMs = clockAfter;
        }

        _moves.Add(new MatchMove(Id, Ply, move.Notation, now, clockAfter));
        Position = next.ToNotation();
        Ply++;
        StartTurn(now);

        if (next.Outcome is { } outcome)
        {
            Finish(outcome.Winner, MatchEndReasonExtensions.FromRules(outcome.Reason), now);
        }

        return MoveResult.Accepted;
    }

    /// <summary><paramref name="playerId"/> resigns; the opponent wins.</summary>
    public bool Resign(Guid playerId, DateTime now)
    {
        if (Status != MatchStatus.Active || SideOf(playerId) is not { } side)
        {
            return false;
        }

        FreezeClock(now);
        Finish(side.Opponent(), MatchEndReason.Resign, now);
        return true;
    }

    /// <summary><paramref name="playerId"/> left (e.g. disconnected too long); the opponent wins.</summary>
    public bool Abandon(Guid playerId, DateTime now)
    {
        if (Status != MatchStatus.Active || SideOf(playerId) is not { } side)
        {
            return false;
        }

        FreezeClock(now);
        Finish(side.Opponent(), MatchEndReason.Abandon, now);
        return true;
    }

    /// <summary>Ends the game on time if the side to move has run out. Returns whether it did.</summary>
    public bool CheckTimeout(DateTime now)
    {
        if (Status != MatchStatus.Active || RemainingMs(SideToMove, now) > 0)
        {
            return false;
        }

        FinishOnTime(now);
        return true;
    }

    /// <summary>Cancels the match without a result (not rated).</summary>
    public bool Abort(DateTime now)
    {
        if (Status is MatchStatus.Finished or MatchStatus.Aborted)
        {
            return false;
        }

        Status = MatchStatus.Aborted;
        ChallengeCode = null;
        TurnStartedAt = null;
        FlagFallsAt = null;
        FinishedAt = now;
        return true;
    }

    /// <summary>The current rules position.</summary>
    public GameState CurrentState() => GameState.FromNotation(Position, RuleSet.ForVersion(RulesVersion));

    public IEnumerable<MatchMove> OrderedMoves() => _moves.OrderBy(m => m.Ply);

    /// <summary>The integration event for a finished match.</summary>
    public MatchFinishedEto ToFinishedEvent()
    {
        if (Status != MatchStatus.Finished || EndReason is null || FinishedAt is null || SouthPlayerId is null || NorthPlayerId is null)
        {
            throw new InvalidOperationException("The match is not finished");
        }

        var duration = StartedAt is { } started ? (long)(FinishedAt.Value - started).TotalMilliseconds : 0;
        return new MatchFinishedEto(Id, RulesVersion, SouthPlayerId.Value, NorthPlayerId.Value, Winner?.Name(), EndReason.Value.Name(), Ply, duration, FinishedAt.Value);
    }

    private void Start(PlayerRef south, PlayerRef north, DateTime now)
    {
        SouthPlayerId = south.PlayerId;
        SouthDisplayName = south.DisplayName;
        SouthRating = south.Rating;
        NorthPlayerId = north.PlayerId;
        NorthDisplayName = north.DisplayName;
        NorthRating = north.Rating;
        Status = MatchStatus.Active;
        ChallengeCode = null;
        StartedAt = now;
        StartTurn(now);
    }

    private void StartTurn(DateTime now)
    {
        TurnStartedAt = now;
        FlagFallsAt = now.AddMilliseconds(SideToMove == Side.South ? SouthClockMs : NorthClockMs);
    }

    private void FreezeClock(DateTime now)
    {
        var remaining = RemainingMs(SideToMove, now);
        if (SideToMove == Side.South)
        {
            SouthClockMs = remaining;
        }
        else
        {
            NorthClockMs = remaining;
        }
    }

    private void FinishOnTime(DateTime now)
    {
        var loser = SideToMove;
        if (loser == Side.South)
        {
            SouthClockMs = 0;
        }
        else
        {
            NorthClockMs = 0;
        }

        Finish(loser.Opponent(), MatchEndReason.Timeout, now);
    }

    private void Finish(Side? winner, MatchEndReason reason, DateTime now)
    {
        Status = MatchStatus.Finished;
        Winner = winner;
        EndReason = reason;
        FinishedAt = now;
        TurnStartedAt = null;
        FlagFallsAt = null;
    }
}
