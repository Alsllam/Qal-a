using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Localization;
using OpenIddict.Validation.AspNetCore;
using Qala.Framework.Application.Security;
using Qala.Framework.Domain.Exceptions;
using Qala.Framework.Domain.Security;
using Qala.Game.Matches.Application.Matches.DTOs;
using Qala.Game.Matches.Domain.Constants;
using Qala.Game.Matches.Domain.Enums;

namespace Qala.Game.Matches.Application.Play;

/// <summary>Server → client messages of <see cref="MatchHub"/>.</summary>
public interface IMatchClient
{
    Task MatchState(MatchDto match);

    Task MoveMade(MoveMadeDto move);

    Task MoveRejected(MoveRejectedDto rejection);

    Task MatchFound(MatchFoundDto found);

    Task MatchEnded(MatchEndedDto ended);

    Task RematchOffered(RematchOfferedDto offer);
}

/// <summary>
/// Real-time play at <c>/hubs/match</c>. The hub only translates calls; the rules, clocks and persistence live in
/// <see cref="IMatchPlayService"/>. The access token comes from the <c>access_token</c> query string on WebSockets.
/// </summary>
[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
public sealed class MatchHub(IMatchPlayService playService, IStringLocalizer localizer) : Hub<IMatchClient>
{
    public const string Path = "/hubs/match";

    public static string GroupName(Guid matchId) => $"match:{matchId:N}";

    /// <summary>Subscribes to a match and sends its state (also used after a reconnect).</summary>
    public async Task JoinMatch(Guid matchId)
    {
        var userId = RequireUserId();
        var canViewAny = Context.User?.HasClaim(QalaClaimTypes.Permission, MatchesPermissions.ViewMatch) == true;
        MatchDto state;
        try
        {
            state = await playService.GetStateAsync(matchId, userId, canViewAny, Context.ConnectionAborted);
        }
        catch (Exception e) when (e is EntityNotFoundException or ForbiddenException)
        {
            throw new HubException(localizer[e is ForbiddenException ? MatchErrors.NotParticipant : Framework.Application.Localization.LocalizationKeys.NotFound]);
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(matchId), Context.ConnectionAborted);
        await Clients.Caller.MatchState(state);
    }

    /// <summary>Plays <paramref name="move"/>; <paramref name="ply"/> must equal the server ply.</summary>
    public async Task MakeMove(Guid matchId, string move, int ply)
    {
        var userId = RequireUserId();
        var result = await playService.MakeMoveAsync(matchId, userId, move, ply, Context.ConnectionAborted);
        switch (result.Status)
        {
            case MoveStatus.Rejected:
                var current = await playService.GetStateAsync(matchId, userId, canViewAny: false, Context.ConnectionAborted);
                await Clients.Caller.MoveRejected(new MoveRejectedDto(matchId, result.ErrorKey ?? MatchErrors.IllegalMove, localizer[result.ErrorKey ?? MatchErrors.IllegalMove], current.Ply));
                break;
            case MoveStatus.Duplicate:
                // An idempotent retry: resend the state so the client can resynchronise.
                await Clients.Caller.MatchState(await playService.GetStateAsync(matchId, userId, canViewAny: false, Context.ConnectionAborted));
                break;
        }
    }

    /// <summary>Resigns the match.</summary>
    public Task Resign(Guid matchId) => playService.ResignAsync(matchId, RequireUserId(), Context.ConnectionAborted);

    /// <summary>Tells the opponent a rematch is wanted. TODO: create the rematch match when both agree.</summary>
    public async Task OfferRematch(Guid matchId)
    {
        var userId = RequireUserId();
        await playService.GetStateAsync(matchId, userId, canViewAny: false, Context.ConnectionAborted);
        await Clients.OthersInGroup(GroupName(matchId)).RematchOffered(new RematchOfferedDto(matchId, userId));
    }

    private Guid RequireUserId() =>
        CurrentUser.GetUserId(Context.User) ?? throw new HubException(localizer[Framework.Application.Localization.LocalizationKeys.Unauthorized]);
}

/// <summary>SignalR user id = the <c>sub</c> claim, so <c>Clients.User(playerId)</c> reaches a player.</summary>
public sealed class SubClaimUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) => CurrentUser.GetUserId(connection?.User)?.ToString();
}

/// <summary>Pushes match events to clients. Abstracted so services can be tested without SignalR.</summary>
public interface IMatchNotifier
{
    Task MoveMadeAsync(MoveMadeDto move, CancellationToken cancellationToken = default);

    Task MatchEndedAsync(MatchEndedDto ended, CancellationToken cancellationToken = default);

    Task MatchFoundAsync(Guid playerId, MatchFoundDto found, CancellationToken cancellationToken = default);
}

public sealed class SignalRMatchNotifier(IHubContext<MatchHub, IMatchClient> hubContext) : IMatchNotifier
{
    public Task MoveMadeAsync(MoveMadeDto move, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(move);
        return hubContext.Clients.Group(MatchHub.GroupName(move.MatchId)).MoveMade(move);
    }

    public Task MatchEndedAsync(MatchEndedDto ended, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ended);
        return hubContext.Clients.Group(MatchHub.GroupName(ended.MatchId)).MatchEnded(ended);
    }

    public Task MatchFoundAsync(Guid playerId, MatchFoundDto found, CancellationToken cancellationToken = default) =>
        hubContext.Clients.User(playerId.ToString()).MatchFound(found);
}
