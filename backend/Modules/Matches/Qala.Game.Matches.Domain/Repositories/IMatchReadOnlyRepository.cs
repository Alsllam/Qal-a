using Qala.Game.Matches.Domain.Enums;
using Qala.Game.Rules;

namespace Qala.Game.Matches.Domain.Repositories;

/// <summary>Projection of a finished match for the balance dashboard.</summary>
public sealed record FinishedMatchSummary(DateTime FinishedAt, Side? Winner, MatchEndReason Reason, int Plies);

/// <summary>Queries that the generic repository does not cover.</summary>
public interface IMatchReadOnlyRepository
{
    /// <summary>Finished (not aborted) matches with <c>from &lt;= FinishedAt &lt; to</c>, optionally for one rules version.</summary>
    Task<List<FinishedMatchSummary>> GetFinishedSummariesAsync(DateTime from, DateTime to, string? rulesVersion, CancellationToken cancellationToken = default);

    /// <summary>Ids of active matches whose side to move has run out of time at <paramref name="now"/>.</summary>
    Task<List<Guid>> GetExpiredMatchIdsAsync(DateTime now, CancellationToken cancellationToken = default);
}
