using System.Globalization;
using MassTransit;
using Microsoft.Extensions.Logging;
using Qala.Framework.Domain.Events;
using Qala.Framework.Domain.Repositories;
using Qala.Game.Players.Domain.Constants;
using Qala.Game.Players.Domain.Entities;
using Qala.Game.Players.Domain.Ratings;

namespace Qala.Game.Players.Application.EventHandlers;

/// <summary>
/// Updates both players' Glicko-2 ratings when a match ends. Idempotent: the match id is stored as a
/// <see cref="RatedMatch"/> in the same transaction, so a redelivered event changes nothing.
/// </summary>
public sealed partial class MatchFinishedConsumer(
    IRepository<Player, Guid> playerRepository,
    IRepository<RatedMatch, Guid> ratedMatchRepository,
    IUnitOfWork unitOfWork,
    IEventPublisher eventPublisher,
    TimeProvider timeProvider,
    ILogger<MatchFinishedConsumer> logger) : IConsumer<MatchFinishedEto>
{
    public async Task Consume(ConsumeContext<MatchFinishedEto> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var message = context.Message;
        var cancellationToken = context.CancellationToken;
        if (await ratedMatchRepository.AnyAsync(r => r.Id == message.MatchId, cancellationToken))
        {
            LogAlreadyRated(logger, message.MatchId);
            return;
        }

        if (message.SouthPlayerId == message.NorthPlayerId)
        {
            LogSelfPlay(logger, message.MatchId);
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        Player south, north;
        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);
            south = await GetOrCreateAsync(message.SouthPlayerId, cancellationToken);
            north = await GetOrCreateAsync(message.NorthPlayerId, cancellationToken);

            var southScore = message.Winner switch
            {
                "south" => 1.0,
                "north" => 0.0,
                _ => 0.5,
            };
            var southBefore = south.Glicko;
            var northBefore = north.Glicko;
            south.ApplyRatedGame(Glicko2Calculator.Update(southBefore, [new Glicko2Game(northBefore, southScore)]), southScore, message.FinishedAt);
            north.ApplyRatedGame(Glicko2Calculator.Update(northBefore, [new Glicko2Game(southBefore, 1 - southScore)]), 1 - southScore, message.FinishedAt);

            await ratedMatchRepository.InsertAsync(new RatedMatch(message.MatchId, now), cancellationToken: cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            await unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            LogFailed(logger, exception, message.MatchId);
            throw;
        }

        LogRated(logger, message.MatchId, south.Rating, north.Rating);
        foreach (var player in new[] { south, north })
        {
            await eventPublisher.PublishAsync(new PlayerRatingChangedEto(player.UserId, player.DisplayName, player.Rating, player.RatingDeviation), cancellationToken);
        }
    }

    private async Task<Player> GetOrCreateAsync(Guid userId, CancellationToken cancellationToken)
    {
        var player = await playerRepository.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (player is not null)
        {
            return player;
        }

        player = new Player(Guid.NewGuid(), userId, string.Create(CultureInfo.InvariantCulture, $"Player-{userId.ToString("N")[..6]}"), PlayerConsts.DefaultLocale);
        await playerRepository.InsertAsync(player, cancellationToken: cancellationToken);
        return player;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Match {MatchId} rated: south {SouthRating:F1}, north {NorthRating:F1}")]
    private static partial void LogRated(ILogger logger, Guid matchId, double southRating, double northRating);

    [LoggerMessage(Level = LogLevel.Information, Message = "Match {MatchId} was already rated; ignoring the redelivery")]
    private static partial void LogAlreadyRated(ILogger logger, Guid matchId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Match {MatchId} has the same player on both sides; not rated")]
    private static partial void LogSelfPlay(ILogger logger, Guid matchId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Rating match {MatchId} failed")]
    private static partial void LogFailed(ILogger logger, Exception exception, Guid matchId);
}
