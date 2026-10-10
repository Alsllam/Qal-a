using Qala.Framework.Application.Dtos;
using Qala.Framework.Domain.Paging;
using Qala.Game.Players.Application.Players.DTOs;

namespace Qala.Game.Players.Application.Players;

/// <summary>Players API (<c>/players-api/players</c> through the BFF).</summary>
public interface IPlayersAppService
{
    /// <summary>The caller's profile, created on the first call.</summary>
    Task<PlayerProfileDto> GetMeAsync(CancellationToken cancellationToken = default);

    /// <summary>Changes the caller's display name, avatar and language.</summary>
    Task UpdateMeAsync(UpdateMyProfileDto input, CancellationToken cancellationToken = default);

    /// <summary>All players (admin console).</summary>
    Task<PagedResultDto<PlayerListDto>> GetListAsync(FilterPlayerDto input, CancellationToken cancellationToken = default);

    /// <summary>One player (admin console).</summary>
    Task<PlayerDto> GetByIdAsync(EntityIdDto<Guid> input, CancellationToken cancellationToken = default);

    /// <summary>Active rated players by rating.</summary>
    Task<PagedResultDto<LeaderboardEntryDto>> GetLeaderboardAsync(PagedRequestDto input, CancellationToken cancellationToken = default);

    /// <summary>Unbans a player (<c>Permissions.Players.ManagePlayer</c>).</summary>
    Task ActivateAsync(EntityIdDto<Guid> input, CancellationToken cancellationToken = default);

    /// <summary>Bans a player and aborts their open matches (<c>Permissions.Players.ManagePlayer</c>).</summary>
    Task DeactivateAsync(EntityIdDto<Guid> input, CancellationToken cancellationToken = default);
}
