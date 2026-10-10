using Qala.Game.Matches.Application.Matchmaking;
using Qala.Game.Matches.Domain.Entities;
using Qala.Game.Matches.Domain.Enums;
using Qala.Game.Matches.Domain.ValueObjects;
using Qala.Game.Rules;

namespace Qala.Game.Matches.Tests.Domain;

public class MatchDomainTests
{
    private static readonly DateTime Start = new(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc);
    private static readonly PlayerRef South = new(Guid.NewGuid(), "South", 1500);
    private static readonly PlayerRef North = new(Guid.NewGuid(), "North", 1500);

    [Theory]
    [InlineData("4+2", 240, 2)]
    [InlineData("10+0", 600, 0)]
    [InlineData(null, 240, 2)]
    public void TimeControlTryParse_ShouldReadMinutesAndSeconds_WhenValid(string? text, int initial, int increment)
    {
        Assert.True(TimeControl.TryParse(text, out var timeControl));
        Assert.Equal(new TimeControl(initial, increment), timeControl);
    }

    [Fact]
    public void RemainingMs_ShouldRunOnlyTheSideToMovesClock_WhenActive()
    {
        var match = Match.CreatePaired(Guid.NewGuid(), South, North, TimeControl.Default, RuleSet.Standard, Start);

        Assert.Equal(230_000, match.RemainingMs(Side.South, Start.AddSeconds(10)));
        Assert.Equal(240_000, match.RemainingMs(Side.North, Start.AddSeconds(10)));
        Assert.Equal(Start.AddMinutes(4), match.FlagFallsAt);
    }

    [Fact]
    public void Abort_ShouldNotProduceFinishedEvent_WhenMatchIsAborted()
    {
        var match = Match.CreatePaired(Guid.NewGuid(), South, North, TimeControl.Default, RuleSet.Standard, Start);

        Assert.True(match.Abort(Start));
        Assert.Equal(MatchStatus.Aborted, match.Status);
        Assert.Throws<InvalidOperationException>(match.ToFinishedEvent);
    }

    [Fact]
    public void QueueWindow_ShouldWidenWithWaitingTime_WhenPlayerWaits()
    {
        var ticket = new QueueTicket(Guid.NewGuid(), Guid.NewGuid(), "A", 1500, "4+2", Start);
        var queue = new InMemoryMatchmakingQueue();
        queue.Enqueue(ticket, Start);

        var far = new QueueTicket(Guid.NewGuid(), Guid.NewGuid(), "B", 1800, "4+2", Start);
        Assert.Null(queue.Enqueue(far, Start.AddSeconds(5)));
        Assert.Equal(150, InMemoryMatchmakingQueue.WindowFor(ticket, Start.AddSeconds(5)));
        Assert.Equal(600, InMemoryMatchmakingQueue.WindowFor(ticket, Start.AddMinutes(10)));

        var paired = queue.Enqueue(new QueueTicket(Guid.NewGuid(), Guid.NewGuid(), "C", 1790, "4+2", Start.AddSeconds(60)), Start.AddSeconds(60));
        Assert.Equal(ticket.TicketId, paired?.TicketId);
    }
}
