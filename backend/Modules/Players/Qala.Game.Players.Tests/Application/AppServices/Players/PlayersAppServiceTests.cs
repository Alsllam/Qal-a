using System.Reflection;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Qala.Framework.Application.Authorization;
using Qala.Framework.Application.Dtos;
using Qala.Framework.Domain.Events;
using Qala.Framework.Domain.Exceptions;
using Qala.Game.Players.Application.Players;
using Qala.Game.Players.Application.Players.DTOs;
using Qala.Game.Players.Domain.Constants;
using Qala.Game.Players.Domain.Entities;
using Qala.Game.Players.Domain.Ratings;
using Qala.Game.Players.Tests.Application.TestInfrastructure;

namespace Qala.Game.Players.Tests.Application.AppServices.Players;

public class PlayersAppServiceTests : IAsyncLifetime
{
    private static readonly Guid Alice = Guid.NewGuid();
    private static readonly Guid Admin = Guid.NewGuid();
    private readonly PlayersTestContext _context = new();

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _context.DisposeAsync();

    [Fact]
    public async Task GetMe_ShouldCreateProfileOnce_WhenCalledTwice()
    {
        // Arrange
        _context.CurrentUser.SignIn(Alice, "Alice");

        // Act
        var first = await _context.AppService.GetMeAsync();
        await _context.NewScopeAsync();
        var second = await _context.AppService.GetMeAsync();

        // Assert
        Assert.Equal(first.Id, second.Id);
        Assert.Equal("Alice", first.DisplayName);
        Assert.Equal(1500, first.Rating);
        Assert.Equal(350, first.RatingDeviation);
        Assert.Single(_context.DbContext.Players);
        A.CallTo(() => _context.EventPublisher.PublishAsync(A<PlayerRatingChangedEto>.That.Matches(e => e.PlayerId == Alice), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task GetMe_ShouldUseGeneratedName_WhenTokenNameIsAnEmail()
    {
        _context.CurrentUser.SignIn(Alice, "alice@example.com");

        var profile = await _context.AppService.GetMeAsync();

        Assert.StartsWith("Player-", profile.DisplayName, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateMe_ShouldChangeProfile_WhenInputIsValid()
    {
        _context.CurrentUser.SignIn(Alice, "Alice");
        await _context.AppService.GetMeAsync();

        await _context.AppService.UpdateMeAsync(new UpdateMyProfileDto { DisplayName = "عليّة", AvatarId = "falcon", Locale = "ar" });

        await _context.NewScopeAsync();
        var profile = await _context.AppService.GetMeAsync();
        Assert.Equal("عليّة", profile.DisplayName);
        Assert.Equal("falcon", profile.AvatarId);
        Assert.Equal("ar", profile.Locale);
    }

    [Theory]
    [InlineData("", "en")]
    [InlineData("A", "en")]
    [InlineData("<script>", "en")]
    [InlineData("Alice", "fr")]
    [InlineData("A name that is far too long for the board", "en")]
    public async Task UpdateMe_ShouldThrowValidationException_WhenInputIsInvalid(string displayName, string locale)
    {
        _context.CurrentUser.SignIn(Alice, "Alice");
        await _context.AppService.GetMeAsync();

        await Assert.ThrowsAsync<ValidationException>(() =>
            _context.AppService.UpdateMeAsync(new UpdateMyProfileDto { DisplayName = displayName, Locale = locale }));
    }

    [Fact]
    public async Task UpdateMe_ShouldThrowNotFound_WhenProfileDoesNotExist()
    {
        _context.CurrentUser.SignIn(Alice);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _context.AppService.UpdateMeAsync(new UpdateMyProfileDto { DisplayName = "Alice", Locale = "en" }));
    }

    [Fact]
    public async Task GetById_ShouldThrowEntityNotFound_WhenPlayerDoesNotExist()
    {
        _context.CurrentUser.SignIn(Admin, "Admin", PlayersPermissions.ViewPlayer);

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _context.AppService.GetByIdAsync(new EntityIdDto<Guid> { Id = Guid.NewGuid() }));
    }

    [Fact]
    public async Task GetList_ShouldFilterByTextAndActive_WhenFiltersGiven()
    {
        // Arrange
        await SeedAsync(("Alice", 1600, 3, true), ("Bob", 1400, 2, true), ("Alina", 1500, 1, false));
        _context.CurrentUser.SignIn(Admin, "Admin", PlayersPermissions.ViewPlayer);

        // Act
        var result = await _context.AppService.GetListAsync(new FilterPlayerDto { FilterText = "Al", ActiveFilter = ActiveFilter.Active });

        // Assert
        Assert.Equal("Alice", Assert.Single(result.Items).DisplayName);
    }

    [Fact]
    public async Task GetLeaderboard_ShouldRankActiveRatedPlayersByRating_WhenCalled()
    {
        await SeedAsync(("Alice", 1600, 3, true), ("Bob", 1700, 2, true), ("Banned", 1900, 9, false), ("Newbie", 1500, 0, true));
        _context.CurrentUser.SignIn(Alice);

        var board = await _context.AppService.GetLeaderboardAsync(new PagedRequestDto());

        Assert.Equal(2, board.TotalCount);
        Assert.Equal([(1, "Bob"), (2, "Alice")], board.Items.Select(e => (e.Rank, e.DisplayName)));
    }

    [Fact]
    public async Task Deactivate_ShouldThrowForbidden_WhenCallerLacksManagePermission()
    {
        var ids = await SeedAsync(("Alice", 1600, 3, true));
        _context.CurrentUser.SignIn(Admin, "Admin", PlayersPermissions.ViewPlayer);

        await Assert.ThrowsAsync<ForbiddenException>(() => _context.AppService.DeactivateAsync(new EntityIdDto<Guid> { Id = ids[0] }));
    }

    [Fact]
    public async Task Deactivate_ShouldBanAndPublishEvent_WhenCallerMayManagePlayers()
    {
        // Arrange
        var ids = await SeedAsync(("Alice", 1600, 3, true));
        _context.CurrentUser.SignIn(Admin, "Admin", PlayersPermissions.ManagePlayer, PlayersPermissions.ViewPlayer);

        // Act
        await _context.AppService.DeactivateAsync(new EntityIdDto<Guid> { Id = ids[0] });

        // Assert
        var player = await _context.AppService.GetByIdAsync(new EntityIdDto<Guid> { Id = ids[0] });
        Assert.False(player.IsActive);
        A.CallTo(() => _context.EventPublisher.PublishAsync(A<PlayerBannedEto>.That.Matches(e => e.PlayerId == player.UserId), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        _context.CurrentUser.SignIn(player.UserId);
        await Assert.ThrowsAsync<ForbiddenException>(() => _context.AppService.GetMeAsync());
    }

    [Theory]
    [InlineData(nameof(PlayersAppService.GetListAsync), PlayersPermissions.ViewPlayer)]
    [InlineData(nameof(PlayersAppService.GetByIdAsync), PlayersPermissions.ViewPlayer)]
    public void Endpoint_ShouldRequirePermission_WhenAdminOnly(string method, string permission)
    {
        var attribute = typeof(PlayersAppService).GetMethod(method)!.GetCustomAttribute<HasPermissionAttribute>();

        Assert.Equal(permission, attribute?.Permission);
    }

    [Fact]
    public void AppService_ShouldExposeActivateDeactivateAndExplicitRoute_WhenDeclared()
    {
        Assert.Equal("players", typeof(PlayersAppService).GetCustomAttribute<RouteAttribute>()!.Template);
        Assert.Equal("activate", typeof(PlayersAppService).GetMethod(nameof(PlayersAppService.ActivateAsync))!.GetCustomAttribute<HttpPostAttribute>()!.Template);
        Assert.Equal("deactivate", typeof(PlayersAppService).GetMethod(nameof(PlayersAppService.DeactivateAsync))!.GetCustomAttribute<HttpPostAttribute>()!.Template);
        Assert.Equal("me", typeof(PlayersAppService).GetMethod(nameof(PlayersAppService.UpdateMeAsync))!.GetCustomAttribute<HttpPutAttribute>()!.Template);
    }

    private async Task<List<Guid>> SeedAsync(params (string Name, double Rating, int Games, bool Active)[] players)
    {
        var ids = new List<Guid>();
        foreach (var (name, rating, games, active) in players)
        {
            var player = new Player(Guid.NewGuid(), Guid.NewGuid(), name, "en") { IsActive = active };
            for (var i = 0; i < games; i++)
            {
                player.ApplyRatedGame(new Glicko2Rating(rating, 100, 0.06), 1, DateTime.UtcNow);
            }

            _context.DbContext.Players.Add(player);
            ids.Add(player.Id);
        }

        await _context.DbContext.SaveChangesAsync();
        await _context.NewScopeAsync();
        return ids;
    }
}
