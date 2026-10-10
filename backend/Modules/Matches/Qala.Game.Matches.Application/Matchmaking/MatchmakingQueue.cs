namespace Qala.Game.Matches.Application.Matchmaking;

/// <summary>A player waiting for an opponent.</summary>
public sealed record QueueTicket(Guid TicketId, Guid PlayerId, string DisplayName, double Rating, string TimeControl, DateTime EnqueuedAt);

/// <summary>Pairs waiting players with the same time control and a close rating.</summary>
public interface IMatchmakingQueue
{
    /// <summary>
    /// Adds <paramref name="ticket"/>, or pairs it at once with a compatible waiting ticket (which is then removed
    /// and returned). A player has at most one ticket: a new one replaces the old.
    /// </summary>
    QueueTicket? Enqueue(QueueTicket ticket, DateTime now);

    /// <summary>Removes a ticket of <paramref name="playerId"/>. Returns false if it was not waiting.</summary>
    bool Cancel(Guid ticketId, Guid playerId);

    int Count { get; }
}

/// <summary>
/// In-memory FIFO queue (MVP: one Matches host instance). The rating window starts at
/// <see cref="BaseWindow"/> and widens by <see cref="WindowGrowthPer10Seconds"/> for every 10 s the waiting player
/// has waited, up to <see cref="MaxWindow"/>. TODO: move to Redis for several instances.
/// </summary>
public sealed class InMemoryMatchmakingQueue : IMatchmakingQueue
{
    public const double BaseWindow = 150;
    public const double WindowGrowthPer10Seconds = 50;
    public const double MaxWindow = 600;

    private readonly List<QueueTicket> _waiting = [];
    private readonly Lock _lock = new();

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _waiting.Count;
            }
        }
    }

    public static double WindowFor(QueueTicket waiting, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(waiting);
        var waitedSeconds = Math.Max(0, (now - waiting.EnqueuedAt).TotalSeconds);
        return Math.Min(MaxWindow, BaseWindow + Math.Floor(waitedSeconds / 10) * WindowGrowthPer10Seconds);
    }

    public QueueTicket? Enqueue(QueueTicket ticket, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        lock (_lock)
        {
            _waiting.RemoveAll(t => t.PlayerId == ticket.PlayerId);
            var opponent = _waiting.FirstOrDefault(t =>
                t.TimeControl == ticket.TimeControl
                && Math.Abs(t.Rating - ticket.Rating) <= WindowFor(t, now));
            if (opponent is not null)
            {
                _waiting.Remove(opponent);
                return opponent;
            }

            _waiting.Add(ticket);
            return null;
        }
    }

    public bool Cancel(Guid ticketId, Guid playerId)
    {
        lock (_lock)
        {
            return _waiting.RemoveAll(t => t.TicketId == ticketId && t.PlayerId == playerId) > 0;
        }
    }
}
