using Qala.Framework.Application.Dtos;
using Qala.Framework.Domain.Paging;
using Qala.Game.Matches.Application.Matches.DTOs;

namespace Qala.Game.Matches.Application.Matches;

/// <summary>Matches API (<c>/matches-api/matches</c> through the BFF).</summary>
public interface IMatchesAppService
{
    /// <summary>A match with moves, clocks and outcome. Participants, or <c>Permissions.Matches.ViewMatch</c>.</summary>
    Task<MatchDto> GetByIdAsync(EntityIdDto<Guid> input, CancellationToken cancellationToken = default);

    /// <summary>The caller's matches, newest first.</summary>
    Task<PagedResultDto<MatchListDto>> GetMineAsync(FilterMyMatchesDto input, CancellationToken cancellationToken = default);

    /// <summary>All matches (admin console).</summary>
    Task<PagedResultDto<MatchListDto>> GetListAsync(FilterMatchDto input, CancellationToken cancellationToken = default);

    /// <summary>Creates a friend challenge and returns its 6-character code.</summary>
    Task<ChallengeCodeDto> CreateChallengeAsync(CreateChallengeDto input, CancellationToken cancellationToken = default);

    /// <summary>Accepts a friend's code; the match starts and both players get <c>MatchFound</c>.</summary>
    Task<MatchDto> AcceptChallengeAsync(AcceptChallengeDto input, CancellationToken cancellationToken = default);

    /// <summary>Joins matchmaking. The match is pushed later through the hub (<c>MatchFound</c>).</summary>
    Task<QueueTicketDto> EnqueueAsync(QueueRequestDto input, CancellationToken cancellationToken = default);

    /// <summary>Leaves matchmaking.</summary>
    Task LeaveQueueAsync(QueueTicketDto input, CancellationToken cancellationToken = default);

    /// <summary>Balance metrics for finished matches in a range (<c>Permissions.Dashboard.ViewBalance</c>).</summary>
    Task<BalanceStatsDto> GetStatsAsync(MatchStatsRequestDto input, CancellationToken cancellationToken = default);
}
