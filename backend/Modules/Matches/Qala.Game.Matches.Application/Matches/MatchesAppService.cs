using System.Security.Cryptography;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Qala.Framework.Application.Authorization;
using Qala.Framework.Application.Dtos;
using Qala.Framework.Application.Localization;
using Qala.Framework.Application.Routing;
using Qala.Framework.Application.Services;
using Qala.Framework.Application.Validation;
using Qala.Framework.Domain.Exceptions;
using Qala.Framework.Domain.Paging;
using Qala.Framework.Domain.Repositories;
using Qala.Framework.Domain.Security;
using Qala.Game.Matches.Application.Matches.DTOs;
using Qala.Game.Matches.Application.Matchmaking;
using Qala.Game.Matches.Application.Play;
using Qala.Game.Matches.Application.Stats;
using Qala.Game.Matches.Domain.Constants;
using Qala.Game.Matches.Domain.Entities;
using Qala.Game.Matches.Domain.Enums;
using Qala.Game.Matches.Domain.Repositories;
using Qala.Game.Matches.Domain.ValueObjects;
using Qala.Game.Rules;

namespace Qala.Game.Matches.Application.Matches;

[Route("matches")]
public class MatchesAppService(
    IRepository<Match, Guid> matchRepository,
    IReadOnlyRepository<Match, Guid> matchReadOnlyRepository,
    IReadOnlyRepository<PlayerRating, Guid> ratingRepository,
    IMatchReadOnlyRepository matchQueries,
    IMatchmakingQueue matchmakingQueue,
    IMatchNotifier notifier,
    IChallengeCodeGenerator codeGenerator,
    ICurrentUser currentUser,
    IStringLocalizer localizer,
    TimeProvider timeProvider,
    IValidator<CreateChallengeDto> createChallengeValidator,
    IValidator<AcceptChallengeDto> acceptChallengeValidator,
    IValidator<QueueRequestDto> queueValidator,
    IValidator<MatchStatsRequestDto> statsValidator) : ApplicationService, IMatchesAppService
{
    /// <summary>Default rating for players the read model has not seen yet (Glicko-2 start).</summary>
    public const double DefaultRating = 1500;

    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;

    /// <inheritdoc />
    [HttpPost(RouteDefinitions.GetById)]
    public async Task<MatchDto> GetByIdAsync(EntityIdDto<Guid> input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var userId = currentUser.GetRequiredId();
        var match = await matchReadOnlyRepository.FindAsync(input.Id, cancellationToken, m => m.Moves)
            ?? throw new EntityNotFoundException(localizer[LocalizationKeys.NotFound]);
        if (!match.IsParticipant(userId) && !currentUser.HasPermission(MatchesPermissions.ViewMatch))
        {
            throw new ForbiddenException(localizer[LocalizationKeys.Forbidden]);
        }

        return match.ToDto(Now, userId);
    }

    /// <inheritdoc />
    [HttpPost("mine")]
    public async Task<PagedResultDto<MatchListDto>> GetMineAsync(FilterMyMatchesDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        input.Normalize();
        var userId = currentUser.GetRequiredId();
        var page = await matchReadOnlyRepository.GetPagedListAsync(
            m => (m.CreatorPlayerId == userId || m.SouthPlayerId == userId || m.NorthPlayerId == userId)
                && (input.Status == null || m.Status == input.Status),
            input.SkipCount,
            input.MaxResultCount,
            input.Sorting,
            cancellationToken);
        return page.Map(m => m.ToListDto());
    }

    /// <inheritdoc />
    [HttpPost(RouteDefinitions.List)]
    [HasPermission(MatchesPermissions.ViewMatch)]
    public async Task<PagedResultDto<MatchListDto>> GetListAsync(FilterMatchDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        input.Normalize();
        var page = await matchReadOnlyRepository.GetPagedListAsync(
            m => (input.Status == null || m.Status == input.Status)
                && (input.RulesVersion == null || m.RulesVersion == input.RulesVersion)
                && (input.PlayerId == null || m.SouthPlayerId == input.PlayerId || m.NorthPlayerId == input.PlayerId || m.CreatorPlayerId == input.PlayerId)
                && (input.From == null || m.CreationTime >= input.From)
                && (input.To == null || m.CreationTime < input.To)
                && (string.IsNullOrEmpty(input.FilterText)
                    || (m.SouthDisplayName != null && m.SouthDisplayName.Contains(input.FilterText))
                    || (m.NorthDisplayName != null && m.NorthDisplayName.Contains(input.FilterText))),
            input.SkipCount,
            input.MaxResultCount,
            input.Sorting,
            cancellationToken);
        return page.Map(m => m.ToListDto());
    }

    /// <inheritdoc />
    [HttpPost("challenge")]
    public async Task<ChallengeCodeDto> CreateChallengeAsync(CreateChallengeDto input, CancellationToken cancellationToken = default)
    {
        await createChallengeValidator.ValidateInputAsync(input, cancellationToken);
        TimeControl.TryParse(input.TimeControl, out var timeControl);
        var creator = await GetPlayerAsync(currentUser.GetRequiredId(), cancellationToken);

        var code = codeGenerator.NewCode();
        for (var attempt = 0; attempt < 5 && await matchReadOnlyRepository.AnyAsync(m => m.ChallengeCode == code, cancellationToken); attempt++)
        {
            code = codeGenerator.NewCode();
        }

        var match = Match.CreateChallenge(Guid.NewGuid(), creator, code, timeControl, RuleSet.Standard);
        await matchRepository.InsertAsync(match, autoSave: true, cancellationToken);
        return new ChallengeCodeDto(match.Id, code);
    }

    /// <inheritdoc />
    [HttpPost("challenge/accept")]
    public async Task<MatchDto> AcceptChallengeAsync(AcceptChallengeDto input, CancellationToken cancellationToken = default)
    {
        await acceptChallengeValidator.ValidateInputAsync(input, cancellationToken);
        var userId = currentUser.GetRequiredId();
        var code = input.Code.Trim().ToUpperInvariant();
        var match = await matchRepository.FirstOrDefaultAsync(m => m.ChallengeCode == code && m.Status == MatchStatus.Waiting, cancellationToken, m => m.Moves)
            ?? throw new NotFoundException(localizer[MatchErrors.ChallengeNotFound]);
        if (match.CreatorPlayerId == userId)
        {
            throw new CustomValidationException(localizer[MatchErrors.OwnChallenge]);
        }

        var opponent = await GetPlayerAsync(userId, cancellationToken);
        match.AcceptChallenge(opponent, creatorPlaysSouth: RandomNumberGenerator.GetInt32(2) == 0, Now);
        await matchRepository.UpdateAsync(match, autoSave: true, cancellationToken);

        await NotifyFoundAsync(match, cancellationToken);
        return match.ToDto(Now, userId);
    }

    /// <inheritdoc />
    [HttpPost("queue")]
    public async Task<QueueTicketDto> EnqueueAsync(QueueRequestDto input, CancellationToken cancellationToken = default)
    {
        await queueValidator.ValidateInputAsync(input, cancellationToken);
        TimeControl.TryParse(input.TimeControl, out var timeControl);
        var player = await GetPlayerAsync(currentUser.GetRequiredId(), cancellationToken);
        var now = Now;
        var ticket = new QueueTicket(Guid.NewGuid(), player.PlayerId, player.DisplayName, player.Rating ?? DefaultRating, timeControl.ToString(), now);

        var opponent = matchmakingQueue.Enqueue(ticket, now);
        if (opponent is null)
        {
            return new QueueTicketDto { TicketId = ticket.TicketId };
        }

        // The player who waited longer plays South half of the time.
        var waitingPlaysSouth = RandomNumberGenerator.GetInt32(2) == 0;
        var waiting = new PlayerRef(opponent.PlayerId, opponent.DisplayName, opponent.Rating);
        var match = Match.CreatePaired(
            Guid.NewGuid(),
            waitingPlaysSouth ? waiting : player,
            waitingPlaysSouth ? player : waiting,
            timeControl,
            RuleSet.Standard,
            now);
        await matchRepository.InsertAsync(match, autoSave: true, cancellationToken);
        await NotifyFoundAsync(match, cancellationToken);
        return new QueueTicketDto { TicketId = ticket.TicketId, MatchId = match.Id };
    }

    /// <inheritdoc />
    [HttpDelete("queue")]
    public Task LeaveQueueAsync(QueueTicketDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!matchmakingQueue.Cancel(input.TicketId, currentUser.GetRequiredId()))
        {
            throw new NotFoundException(localizer[MatchErrors.TicketNotFound]);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    [HttpPost("stats")]
    [HasPermission(MatchesPermissions.ViewBalance)]
    public async Task<BalanceStatsDto> GetStatsAsync(MatchStatsRequestDto input, CancellationToken cancellationToken = default)
    {
        await statsValidator.ValidateInputAsync(input, cancellationToken);
        var from = DateTime.SpecifyKind(input.From, DateTimeKind.Utc);
        var to = DateTime.SpecifyKind(input.To, DateTimeKind.Utc);
        var summaries = await matchQueries.GetFinishedSummariesAsync(from, to, input.RulesVersion, cancellationToken);
        return BalanceStatsCalculator.Calculate(from, to, input.RulesVersion, summaries);
    }

    private async Task<PlayerRef> GetPlayerAsync(Guid userId, CancellationToken cancellationToken)
    {
        var rating = await ratingRepository.FindAsync(userId, cancellationToken);
        return rating is not null
            ? new PlayerRef(userId, rating.DisplayName, rating.Rating)
            : new PlayerRef(userId, currentUser.UserName ?? "Player", DefaultRating);
    }

    private async Task NotifyFoundAsync(Match match, CancellationToken cancellationToken)
    {
        await notifier.MatchFoundAsync(match.SouthPlayerId!.Value, new MatchFoundDto(match.Id, Side.South.Name()), cancellationToken);
        await notifier.MatchFoundAsync(match.NorthPlayerId!.Value, new MatchFoundDto(match.Id, Side.North.Name()), cancellationToken);
    }
}

/// <summary>Makes challenge codes. Abstracted for tests.</summary>
public interface IChallengeCodeGenerator
{
    string NewCode();
}

public sealed class RandomChallengeCodeGenerator : IChallengeCodeGenerator
{
    public string NewCode() => RandomNumberGenerator.GetString(MatchConsts.ChallengeCodeAlphabet, MatchConsts.ChallengeCodeLength);
}
