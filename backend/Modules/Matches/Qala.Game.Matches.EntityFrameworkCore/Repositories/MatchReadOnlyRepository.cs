using Microsoft.EntityFrameworkCore;
using Qala.Game.Matches.Domain.Enums;
using Qala.Game.Matches.Domain.Repositories;

namespace Qala.Game.Matches.EntityFrameworkCore.Repositories;

public sealed class MatchReadOnlyRepository(MatchesDbContext dbContext) : IMatchReadOnlyRepository
{
    public Task<List<FinishedMatchSummary>> GetFinishedSummariesAsync(DateTime from, DateTime to, string? rulesVersion, CancellationToken cancellationToken = default) =>
        dbContext.Matches.AsNoTracking()
            .Where(m => m.Status == MatchStatus.Finished
                && m.FinishedAt >= from
                && m.FinishedAt < to
                && m.EndReason != null
                && (rulesVersion == null || m.RulesVersion == rulesVersion))
            .Select(m => new FinishedMatchSummary(m.FinishedAt!.Value, m.Winner, m.EndReason!.Value, m.Ply))
            .ToListAsync(cancellationToken);

    public Task<List<Guid>> GetExpiredMatchIdsAsync(DateTime now, CancellationToken cancellationToken = default) =>
        dbContext.Matches.AsNoTracking()
            .Where(m => m.Status == MatchStatus.Active && m.FlagFallsAt != null && m.FlagFallsAt <= now)
            .Select(m => m.Id)
            .Take(200)
            .ToListAsync(cancellationToken);
}
