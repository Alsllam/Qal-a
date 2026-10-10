using System.Globalization;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Qala.Framework.Application.Authorization;
using Qala.Framework.Application.Dtos;
using Qala.Framework.Application.Localization;
using Qala.Framework.Application.Routing;
using Qala.Framework.Application.Services;
using Qala.Framework.Application.Validation;
using Qala.Framework.Domain.Constants;
using Qala.Framework.Domain.Events;
using Qala.Framework.Domain.Exceptions;
using Qala.Framework.Domain.Paging;
using Qala.Framework.Domain.Repositories;
using Qala.Framework.Domain.Security;
using Qala.Game.Players.Application.Players.DTOs;
using Qala.Game.Players.Domain.Constants;
using Qala.Game.Players.Domain.Entities;

namespace Qala.Game.Players.Application.Players;

[Route("players")]
public class PlayersAppService : ActivableAppService<Player, Guid>, IPlayersAppService
{
    private readonly IRepository<Player, Guid> _playerRepository;
    private readonly IReadOnlyRepository<Player, Guid> _playerReadOnlyRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ICurrentUser _currentUser;
    private readonly IStringLocalizer _localizer;
    private readonly IValidator<UpdateMyProfileDto> _updateValidator;

    public PlayersAppService(
        IRepository<Player, Guid> playerRepository,
        IReadOnlyRepository<Player, Guid> playerReadOnlyRepository,
        IEventPublisher eventPublisher,
        ICurrentUser currentUser,
        IStringLocalizer localizer,
        IValidator<UpdateMyProfileDto> updateValidator)
        : base(playerRepository, currentUser, localizer)
    {
        _playerRepository = playerRepository;
        _playerReadOnlyRepository = playerReadOnlyRepository;
        _eventPublisher = eventPublisher;
        _currentUser = currentUser;
        _localizer = localizer;
        _updateValidator = updateValidator;
    }

    protected override string ActivationPermission => PlayersPermissions.ManagePlayer;

    /// <inheritdoc />
    [HttpPost(RouteDefinitions.Me)]
    public async Task<PlayerProfileDto> GetMeAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.GetRequiredId();
        var player = await _playerReadOnlyRepository.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (player is null)
        {
            player = new Player(Guid.NewGuid(), userId, DefaultDisplayName(userId), CurrentLocale());
            await _playerRepository.InsertAsync(player, autoSave: true, cancellationToken);
            await PublishRatingAsync(player, cancellationToken);
        }

        EnsureNotBanned(player);
        return player.ToProfileDto();
    }

    /// <inheritdoc />
    [HttpPut(RouteDefinitions.Me)]
    public async Task UpdateMeAsync(UpdateMyProfileDto input, CancellationToken cancellationToken = default)
    {
        await _updateValidator.ValidateInputAsync(input, cancellationToken);
        var userId = _currentUser.GetRequiredId();
        var player = await _playerRepository.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken)
            ?? throw new NotFoundException(_localizer[PlayerErrors.ProfileNotFound]);
        EnsureNotBanned(player);

        var nameChanged = player.DisplayName != input.DisplayName.Trim();
        player.UpdateProfile(input.DisplayName.Trim(), input.AvatarId, input.Locale);
        await _playerRepository.UpdateAsync(player, autoSave: true, cancellationToken);
        if (nameChanged)
        {
            await PublishRatingAsync(player, cancellationToken);
        }
    }

    /// <inheritdoc />
    [HttpPost(RouteDefinitions.List)]
    [HasPermission(PlayersPermissions.ViewPlayer)]
    public async Task<PagedResultDto<PlayerListDto>> GetListAsync(FilterPlayerDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        input.ActiveFilter ??= ActiveFilter.All;
        input.Normalize();
        var page = await _playerReadOnlyRepository.GetPagedListAsync(
            x => (string.IsNullOrEmpty(input.FilterText) || x.DisplayName.Contains(input.FilterText))
                && (input.MinRating == null || x.Rating >= input.MinRating)
                && (input.MaxRating == null || x.Rating <= input.MaxRating)
                && (input.ActiveFilter == ActiveFilter.All
                    || (input.ActiveFilter == ActiveFilter.Active && x.IsActive)
                    || (input.ActiveFilter == ActiveFilter.InActive && !x.IsActive)),
            input.SkipCount,
            input.MaxResultCount,
            input.Sorting,
            cancellationToken);
        return page.Map(p => p.ToListDto());
    }

    /// <inheritdoc />
    [HttpPost(RouteDefinitions.GetById)]
    [HasPermission(PlayersPermissions.ViewPlayer)]
    public async Task<PlayerDto> GetByIdAsync(EntityIdDto<Guid> input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var player = await _playerReadOnlyRepository.FindAsync(input.Id, cancellationToken)
            ?? throw new EntityNotFoundException(_localizer[LocalizationKeys.NotFound]);
        return player.ToDto();
    }

    /// <inheritdoc />
    [HttpPost("leaderboard")]
    public async Task<PagedResultDto<LeaderboardEntryDto>> GetLeaderboardAsync(PagedRequestDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        input.Normalize("Rating Desc, GamesPlayed Desc");
        input.Sorting = "Rating Desc, GamesPlayed Desc";
        var page = await _playerReadOnlyRepository.GetPagedListAsync(
            x => x.IsActive && x.GamesPlayed > 0,
            input.SkipCount,
            input.MaxResultCount,
            input.Sorting,
            cancellationToken);
        var rank = input.SkipCount;
        return new PagedResultDto<LeaderboardEntryDto>(
            page.Items.Select(p => new LeaderboardEntryDto(++rank, p.UserId, p.DisplayName, (int)Math.Round(p.Rating), p.GamesPlayed)).ToList(),
            page.TotalCount);
    }

    /// <summary>A ban takes effect in Matches through <see cref="PlayerBannedEto"/>.</summary>
    protected override async Task OnActiveChangedAsync(Player entity, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (!entity.IsActive)
        {
            await _eventPublisher.PublishAsync(new PlayerBannedEto(entity.UserId), cancellationToken);
        }
    }

    private void EnsureNotBanned(Player player)
    {
        if (!player.IsActive)
        {
            throw new ForbiddenException(_localizer[PlayerErrors.Banned]);
        }
    }

    private Task PublishRatingAsync(Player player, CancellationToken cancellationToken) =>
        _eventPublisher.PublishAsync(new PlayerRatingChangedEto(player.UserId, player.DisplayName, player.Rating, player.RatingDeviation), cancellationToken);

    private string DefaultDisplayName(Guid userId)
    {
        var name = _currentUser.UserName?.Trim();
        if (!string.IsNullOrEmpty(name) && name.Length <= FieldDefinitions.MaxDisplayNameLength && !name.Contains('@', StringComparison.Ordinal))
        {
            return name;
        }

        return string.Create(CultureInfo.InvariantCulture, $"Player-{userId.ToString("N")[..6]}");
    }

    private static string CurrentLocale()
    {
        var language = JsonStringLocalizer.CurrentLanguage;
        return FieldDefinitions.SupportedLocales.Contains(language) ? language : PlayerConsts.DefaultLocale;
    }
}
