using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Qala.Framework.Domain.Events;
using Qala.Framework.Domain.Security;
using Qala.Framework.EntityFrameworkCore.Extensions;
using Qala.Framework.EntityFrameworkCore.Messaging;
using Qala.Game.Players.Application.EventHandlers;
using Qala.Game.Players.EntityFrameworkCore;
using Qala.Game.Players.Tests.Application.TestInfrastructure;

namespace Qala.Game.Players.Tests.Application.EventHandlers;

/// <summary>Runs the consumer on MassTransit's in-memory test harness, as RabbitMQ would deliver it.</summary>
public class MatchFinishedConsumerTests : IAsyncLifetime
{
    private static readonly Guid South = Guid.NewGuid();
    private static readonly Guid North = Guid.NewGuid();
    private readonly string _databaseName = $"players-{Guid.NewGuid():N}";
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<PlayersDbContext>(o => o.UseInMemoryDatabase(_databaseName));
        services.AddPlayersRepositories();
        services.AddSharedRepositories();
        services.AddSingleton<ICurrentUser>(new TestCurrentUser());
        services.AddScoped<IEventPublisher, MassTransitEventPublisher>();
        services.AddMassTransitTestHarness(x => x.AddConsumer<MatchFinishedConsumer>());
        _provider = services.BuildServiceProvider(validateScopes: true);
        _harness = _provider.GetRequiredService<ITestHarness>();
        await _harness.Start();
    }

    public async Task DisposeAsync()
    {
        await _harness.Stop();
        await _provider.DisposeAsync();
    }

    [Fact]
    public async Task Consume_ShouldUpdateBothRatings_WhenSouthWins()
    {
        // Act
        await PublishAndWaitAsync(Finished(Guid.NewGuid(), "south"), expectedConsumed: 1);

        // Assert
        await using var scope = _provider.CreateAsyncScope();
        var players = await scope.ServiceProvider.GetRequiredService<PlayersDbContext>().Players.ToDictionaryAsync(p => p.UserId);
        Assert.True(players[South].Rating > 1500);
        Assert.True(players[North].Rating < 1500);
        Assert.Equal(players[South].Rating - 1500, 1500 - players[North].Rating, 6);
        Assert.Equal((1, 1, 0), (players[South].GamesPlayed, players[South].Wins, players[South].Losses));
        Assert.Equal((1, 0, 1), (players[North].GamesPlayed, players[North].Wins, players[North].Losses));
        Assert.True(await _harness.Published.Any<PlayerRatingChangedEto>(x => x.Context.Message.PlayerId == South));
    }

    [Fact]
    public async Task Consume_ShouldRateOnce_WhenSameMatchIsDeliveredTwice()
    {
        // Arrange
        var matchId = Guid.NewGuid();

        // Act
        await PublishAndWaitAsync(Finished(matchId, "north"), expectedConsumed: 1);
        await PublishAndWaitAsync(Finished(matchId, "north"), expectedConsumed: 2);

        // Assert
        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PlayersDbContext>();
        Assert.All(await db.Players.ToListAsync(), p => Assert.Equal(1, p.GamesPlayed));
        Assert.Single(await db.RatedMatches.ToListAsync());
    }

    [Fact]
    public async Task Consume_ShouldKeepEqualRatings_WhenGameIsDrawn()
    {
        await PublishAndWaitAsync(Finished(Guid.NewGuid(), null), expectedConsumed: 1);

        await using var scope = _provider.CreateAsyncScope();
        var players = await scope.ServiceProvider.GetRequiredService<PlayersDbContext>().Players.ToListAsync();
        Assert.All(players, p => Assert.Equal(1500, p.Rating, 6));
        Assert.All(players, p => Assert.Equal(1, p.Draws));
    }

    private static MatchFinishedEto Finished(Guid matchId, string? winner) =>
        new(matchId, "0.6", South, North, winner, winner is null ? "plyLimitDraw" : "amirCaptured", 30, 300_000, DateTime.UtcNow);

    private async Task PublishAndWaitAsync(MatchFinishedEto message, int expectedConsumed)
    {
        await _harness.Bus.Publish(message);
        var consumer = _harness.GetConsumerHarness<MatchFinishedConsumer>();
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (consumer.Consumed.Select<MatchFinishedEto>().Count() < expectedConsumed && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
        }

        Assert.Equal(expectedConsumed, consumer.Consumed.Select<MatchFinishedEto>().Count());
        Assert.False(await _harness.Consumed.Any<MatchFinishedEto>(x => x.Exception is not null));
    }
}
