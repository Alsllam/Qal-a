using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Qala.Framework.Domain.Events;
using Qala.Framework.Domain.Exceptions;
using Qala.Framework.Domain.Repositories;
using Qala.Game.Matches.Application.Matches;
using Qala.Game.Matches.Application.Matches.DTOs;
using Qala.Game.Matches.Domain.Constants;
using Qala.Game.Matches.Domain.Entities;
using Qala.Game.Matches.Domain.Enums;
using Qala.Game.Matches.Domain.Repositories;

namespace Qala.Game.Matches.Application.Play;

/// <summary>Server-authoritative play: moves, resignations and clocks. Used by <see cref="MatchHub"/> and the clock sweeper.</summary>
public interface IMatchPlayService
{
    /// <summary>The match state for a participant (or anyone with <c>ViewMatch</c>).</summary>
    Task<MatchDto> GetStateAsync(Guid matchId, Guid userId, bool canViewAny, CancellationToken cancellationToken = default);

    /// <summary>Validates and plays a move; broadcasts it and, if the game ended, publishes <see cref="MatchFinishedEto"/>.</summary>
    Task<MakeMoveResultDto> MakeMoveAsync(Guid matchId, Guid userId, string move, int ply, CancellationToken cancellationToken = default);

    Task<bool> ResignAsync(Guid matchId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Ends every active match whose side to move has run out of time. Returns how many ended.</summary>
    Task<int> ExpireClocksAsync(CancellationToken cancellationToken = default);
}

public sealed partial class MatchPlayService(
    IRepository<Match, Guid> matchRepository,
    IMatchReadOnlyRepository matchReadOnlyRepository,
    IEventPublisher eventPublisher,
    IMatchNotifier notifier,
    TimeProvider timeProvider,
    ILogger<MatchPlayService> logger) : IMatchPlayService
{
    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;

    public async Task<MatchDto> GetStateAsync(Guid matchId, Guid userId, bool canViewAny, CancellationToken cancellationToken = default)
    {
        var match = await matchRepository.FindAsync(matchId, cancellationToken, m => m.Moves)
            ?? throw new EntityNotFoundException(typeof(Match), matchId);
        if (!canViewAny && !match.IsParticipant(userId))
        {
            throw new ForbiddenException(MatchErrors.NotParticipant);
        }

        return match.ToDto(Now, userId);
    }

    public async Task<MakeMoveResultDto> MakeMoveAsync(Guid matchId, Guid userId, string move, int ply, CancellationToken cancellationToken = default)
    {
        var match = await matchRepository.FindAsync(matchId, cancellationToken, m => m.Moves);
        if (match is null)
        {
            return new MakeMoveResultDto(MoveStatus.Rejected, MatchErrors.NotActive, null);
        }

        var now = Now;
        var wasActive = match.Status == MatchStatus.Active;
        var result = match.ApplyMove(userId, move, ply, now);
        if (result.Status == MoveStatus.Duplicate || (result.Status == MoveStatus.Rejected && !(wasActive && match.Status == MatchStatus.Finished)))
        {
            LogMoveNotPlayed(logger, matchId, ply, result.Status, result.ErrorKey);
            return new MakeMoveResultDto(result.Status, result.ErrorKey, null);
        }

        try
        {
            await matchRepository.UpdateAsync(match, autoSave: true, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another move for this match was saved first (double submit from two devices).
            return new MakeMoveResultDto(MoveStatus.Rejected, MatchErrors.Conflict, null);
        }

        MoveMadeDto? moveMade = null;
        if (result.Status == MoveStatus.Accepted)
        {
            moveMade = new MoveMadeDto(match.Id, move, match.Ply - 1, match.Position, match.ToClocksDto(now), match.ToOutcomeDto());
            await notifier.MoveMadeAsync(moveMade, cancellationToken);
        }

        if (match.Status == MatchStatus.Finished)
        {
            await OnFinishedAsync(match, cancellationToken);
        }

        return new MakeMoveResultDto(result.Status, result.ErrorKey, moveMade);
    }

    public async Task<bool> ResignAsync(Guid matchId, Guid userId, CancellationToken cancellationToken = default)
    {
        var match = await matchRepository.FindAsync(matchId, cancellationToken)
            ?? throw new EntityNotFoundException(typeof(Match), matchId);
        if (!match.Resign(userId, Now))
        {
            return false;
        }

        await matchRepository.UpdateAsync(match, autoSave: true, cancellationToken);
        await OnFinishedAsync(match, cancellationToken);
        return true;
    }

    public async Task<int> ExpireClocksAsync(CancellationToken cancellationToken = default)
    {
        var now = Now;
        var ended = 0;
        foreach (var id in await matchReadOnlyRepository.GetExpiredMatchIdsAsync(now, cancellationToken))
        {
            var match = await matchRepository.FindAsync(id, cancellationToken);
            if (match is null || !match.CheckTimeout(now))
            {
                continue;
            }

            try
            {
                await matchRepository.UpdateAsync(match, autoSave: true, cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                // A move landed at the same moment; the next sweep re-checks this match.
                continue;
            }

            await OnFinishedAsync(match, cancellationToken);
            ended++;
        }

        return ended;
    }

    /// <summary>After the save: tell the clients and publish the integration event.</summary>
    private async Task OnFinishedAsync(Match match, CancellationToken cancellationToken)
    {
        var finished = match.ToFinishedEvent();
        LogMatchFinished(logger, match.Id, finished.Winner, finished.Reason, finished.Plies);
        await notifier.MatchEndedAsync(new MatchEndedDto(match.Id, match.ToOutcomeDto()), cancellationToken);
        await eventPublisher.PublishAsync(finished, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Match {MatchId} finished: winner {Winner}, reason {Reason}, {Plies} plies")]
    private static partial void LogMatchFinished(ILogger logger, Guid matchId, string? winner, string reason, int plies);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Move for match {MatchId} at ply {Ply} not played: {Status} {ErrorKey}")]
    private static partial void LogMoveNotPlayed(ILogger logger, Guid matchId, int ply, MoveStatus status, string? errorKey);
}
