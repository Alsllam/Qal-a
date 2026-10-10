using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using Qala.Framework.Domain.Events;
using Qala.Framework.Domain.Repositories;
using Qala.Game.Matches.Application.EventHandlers;
using Qala.Game.Matches.Application.Matches.DTOs;
using Qala.Game.Matches.Domain.Entities;
using Qala.Game.Matches.Domain.Enums;
using Qala.Game.Matches.Tests.Application.TestInfrastructure;
using static Qala.Game.Matches.Tests.Application.TestInfrastructure.MatchBuilder;

namespace Qala.Game.Matches.Tests.Application.EventHandlers;

public class PlayerEventConsumersTests : IAsyncLifetime
{
    private readonly MatchesTestContext _context = new();

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _context.DisposeAsync();

    [Fact]
    public async Task PlayerBanned_ShouldAbortActiveMatches_WhenPlayerIsBanned()
    {
        // Arrange
        var match = await _context.StartMatchAsync();
        var consumer = new PlayerBannedConsumer(_context.Get<IRepository<Match, Guid>>(), _context.Notifier, _context.Clock, NullLogger<PlayerBannedConsumer>.Instance);

        // Act
        await consumer.Consume(Context(new PlayerBannedEto(Bob)));
        await consumer.Consume(Context(new PlayerBannedEto(Bob))); // redelivery: nothing left to abort

        // Assert
        await _context.NewScopeAsync();
        var stored = await _context.Get<IReadOnlyRepository<Match, Guid>>().FindAsync(match.Id);
        Assert.Equal(MatchStatus.Aborted, stored!.Status);
        A.CallTo(() => _context.Notifier.MatchEndedAsync(A<MatchEndedDto>.That.Matches(e => e.MatchId == match.Id && e.Outcome == null), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _context.EventPublisher.PublishAsync(A<MatchFinishedEto>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task PlayerRatingChanged_ShouldUpsertReadModel_WhenReceivedTwice()
    {
        var consumer = new PlayerRatingChangedConsumer(_context.Get<IRepository<PlayerRating, Guid>>(), _context.Clock, NullLogger<PlayerRatingChangedConsumer>.Instance);

        await consumer.Consume(Context(new PlayerRatingChangedEto(Alice, "Alice", 1520, 200)));
        await consumer.Consume(Context(new PlayerRatingChangedEto(Alice, "Alice A.", 1540, 190)));

        var rating = Assert.Single(_context.DbContext.PlayerRatings);
        Assert.Equal(1540, rating.Rating);
        Assert.Equal("Alice A.", rating.DisplayName);
    }

    private static ConsumeContext<T> Context<T>(T message)
        where T : class
    {
        var context = A.Fake<ConsumeContext<T>>();
        A.CallTo(() => context.Message).Returns(message);
        A.CallTo(() => context.CancellationToken).Returns(CancellationToken.None);
        return context;
    }
}
