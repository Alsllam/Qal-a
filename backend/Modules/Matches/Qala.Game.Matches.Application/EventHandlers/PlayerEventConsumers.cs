using MassTransit;
using Microsoft.Extensions.Logging;
using Qala.Framework.Domain.Events;
using Qala.Framework.Domain.Repositories;
using Qala.Game.Matches.Application.Matches.DTOs;
using Qala.Game.Matches.Application.Play;
using Qala.Game.Matches.Domain.Entities;
using Qala.Game.Matches.Domain.Enums;

namespace Qala.Game.Matches.Application.EventHandlers;

/// <summary>Keeps the <see cref="PlayerRating"/> read model in step with Players. Idempotent (an upsert).</summary>
public sealed partial class PlayerRatingChangedConsumer(
    IRepository<PlayerRating, Guid> repository,
    TimeProvider timeProvider,
    ILogger<PlayerRatingChangedConsumer> logger) : IConsumer<PlayerRatingChangedEto>
{
    public async Task Consume(ConsumeContext<PlayerRatingChangedEto> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var message = context.Message;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        try
        {
            var existing = await repository.FindAsync(message.PlayerId, context.CancellationToken);
            if (existing is null)
            {
                await repository.InsertAsync(new PlayerRating(message.PlayerId, message.DisplayName, message.Rating, message.RatingDeviation, now), autoSave: true, context.CancellationToken);
            }
            else
            {
                existing.Update(message.DisplayName, message.Rating, message.RatingDeviation, now);
                await repository.UpdateAsync(existing, autoSave: true, context.CancellationToken);
            }
        }
        catch (Exception exception)
        {
            LogFailed(logger, exception, message.PlayerId);
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Updating the rating read model failed for player {PlayerId}")]
    private static partial void LogFailed(ILogger logger, Exception exception, Guid playerId);
}

/// <summary>Aborts (unrated) every waiting or active match of a banned player. Idempotent: finished matches are skipped.</summary>
public sealed partial class PlayerBannedConsumer(
    IRepository<Match, Guid> repository,
    IMatchNotifier notifier,
    TimeProvider timeProvider,
    ILogger<PlayerBannedConsumer> logger) : IConsumer<PlayerBannedEto>
{
    public async Task Consume(ConsumeContext<PlayerBannedEto> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var playerId = context.Message.PlayerId;
        try
        {
            var open = await repository.GetListAsync(
                m => (m.Status == MatchStatus.Waiting || m.Status == MatchStatus.Active)
                    && (m.CreatorPlayerId == playerId || m.SouthPlayerId == playerId || m.NorthPlayerId == playerId),
                context.CancellationToken);
            var now = timeProvider.GetUtcNow().UtcDateTime;
            foreach (var match in open.Where(m => m.Abort(now)))
            {
                await repository.UpdateAsync(match, autoSave: true, context.CancellationToken);
                await notifier.MatchEndedAsync(new MatchEndedDto(match.Id, null), context.CancellationToken);
            }

            LogAborted(logger, open.Count, playerId);
        }
        catch (Exception exception)
        {
            LogFailed(logger, exception, playerId);
            throw;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Aborted {Count} matches of banned player {PlayerId}")]
    private static partial void LogAborted(ILogger logger, int count, Guid playerId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Aborting matches failed for banned player {PlayerId}")]
    private static partial void LogFailed(ILogger logger, Exception exception, Guid playerId);
}
