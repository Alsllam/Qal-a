using System.Reflection;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Qala.Framework.Application.Authorization;
using Qala.Framework.Application.Dtos;
using Qala.Framework.Application.Services;
using Qala.Framework.Domain.Exceptions;
using Qala.Game.Matches.Application.Matches;
using Qala.Game.Matches.Application.Matches.DTOs;
using Qala.Game.Matches.Application.Play;
using Qala.Game.Matches.Domain.Constants;
using Qala.Game.Matches.Domain.Entities;
using Qala.Game.Matches.Domain.Enums;
using Qala.Game.Matches.Tests.Application.TestInfrastructure;
using static Qala.Game.Matches.Tests.Application.TestInfrastructure.MatchBuilder;

namespace Qala.Game.Matches.Tests.Application.AppServices.Matches;

public class MatchesAppServiceTests : IAsyncLifetime
{
    private readonly MatchesTestContext _context = new();

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _context.DisposeAsync();

    [Fact]
    public async Task CreateChallenge_ShouldReturnCodeAndWaitingMatch_WhenTimeControlIsValid()
    {
        // Arrange
        _context.CurrentUser.SignIn(Alice, "Alice");

        // Act
        var result = await _context.AppService.CreateChallengeAsync(new CreateChallengeDto { TimeControl = "4+2" });

        // Assert
        Assert.Matches(MatchConsts.ChallengeCodePattern, result.Code);
        var match = await _context.AppService.GetByIdAsync(new EntityIdDto<Guid> { Id = result.MatchId });
        Assert.Equal(MatchStatus.Waiting, match.Status);
        Assert.Equal(result.Code, match.ChallengeCode);
        Assert.Equal("0.6", match.RulesVersion);
        Assert.Equal("1fjajf1/2jrj2/7/7/7/2JRJ2/1FJAJF1 s 0 0:0", match.Position);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0+2")]
    [InlineData("4-2")]
    [InlineData("99+1")]
    public async Task CreateChallenge_ShouldThrowValidationException_WhenTimeControlIsInvalid(string timeControl)
    {
        _context.CurrentUser.SignIn(Alice);

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            _context.AppService.CreateChallengeAsync(new CreateChallengeDto { TimeControl = timeControl }));

        Assert.Contains(exception.Errors, e => e.ErrorMessage == MatchErrors.InvalidTimeControl);
    }

    [Fact]
    public async Task AcceptChallenge_ShouldStartMatchWithClocks_WhenCodeIsValid()
    {
        // Arrange
        _context.CurrentUser.SignIn(Alice, "Alice");
        var challenge = await _context.AppService.CreateChallengeAsync(new CreateChallengeDto());
        _context.CurrentUser.SignIn(Bob, "Bob");

        // Act
        var match = await _context.AppService.AcceptChallengeAsync(new AcceptChallengeDto { Code = challenge.Code.ToLowerInvariant() });

        // Assert
        Assert.Equal(MatchStatus.Active, match.Status);
        Assert.Equal(new HashSet<Guid> { Alice, Bob }, new HashSet<Guid> { match.SouthOf(), match.NorthOf() });
        Assert.Equal(new MatchClocksDto(240_000, 240_000, 2_000), match.Clocks);
        Assert.Equal("south", match.ToMove);
        Assert.Null(match.ChallengeCode);
        A.CallTo(() => _context.Notifier.MatchFoundAsync(A<Guid>._, A<MatchFoundDto>.That.Matches(f => f.MatchId == match.Id), A<CancellationToken>._))
            .MustHaveHappenedTwiceExactly();
    }

    [Fact]
    public async Task AcceptChallenge_ShouldThrowCustomValidation_WhenAcceptingOwnChallenge()
    {
        _context.CurrentUser.SignIn(Alice);
        var challenge = await _context.AppService.CreateChallengeAsync(new CreateChallengeDto());

        await Assert.ThrowsAsync<CustomValidationException>(() =>
            _context.AppService.AcceptChallengeAsync(new AcceptChallengeDto { Code = challenge.Code }));
    }

    [Fact]
    public async Task AcceptChallenge_ShouldThrowNotFound_WhenCodeIsUnknown()
    {
        _context.CurrentUser.SignIn(Bob);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _context.AppService.AcceptChallengeAsync(new AcceptChallengeDto { Code = "ZZZZZZ" }));
    }

    [Fact]
    public async Task AcceptChallenge_ShouldThrowValidationException_WhenCodeIsMalformed()
    {
        _context.CurrentUser.SignIn(Bob);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _context.AppService.AcceptChallengeAsync(new AcceptChallengeDto { Code = "12" }));
    }

    [Fact]
    public async Task GetById_ShouldThrowEntityNotFound_WhenMatchDoesNotExist()
    {
        _context.CurrentUser.SignIn(Alice);

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            _context.AppService.GetByIdAsync(new EntityIdDto<Guid> { Id = Guid.NewGuid() }));
    }

    [Fact]
    public async Task GetById_ShouldThrowForbidden_WhenCallerIsNeitherPlayerNorViewer()
    {
        var match = await _context.StartMatchAsync();
        _context.CurrentUser.SignIn(Carol);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _context.AppService.GetByIdAsync(new EntityIdDto<Guid> { Id = match.Id }));
    }

    [Fact]
    public async Task GetById_ShouldReturnMatch_WhenCallerHasViewMatchPermission()
    {
        var match = await _context.StartMatchAsync();
        _context.CurrentUser.SignIn(Carol, "Carol", MatchesPermissions.ViewMatch);

        var result = await _context.AppService.GetByIdAsync(new EntityIdDto<Guid> { Id = match.Id });

        Assert.Equal(match.Id, result.Id);
    }

    [Fact]
    public async Task GetMine_ShouldReturnOnlyCallersMatches_WhenOthersExist()
    {
        // Arrange: Alice-Bob match, plus a Carol challenge.
        var match = await _context.StartMatchAsync();
        _context.CurrentUser.SignIn(Carol);
        await _context.AppService.CreateChallengeAsync(new CreateChallengeDto());

        // Act
        _context.CurrentUser.SignIn(Bob);
        var mine = await _context.AppService.GetMineAsync(new FilterMyMatchesDto());

        // Assert
        Assert.Equal(1, mine.TotalCount);
        Assert.Equal(match.Id, Assert.Single(mine.Items).Id);
    }

    [Fact]
    public async Task GetList_ShouldFilterByStatus_WhenStatusGiven()
    {
        await _context.StartMatchAsync();
        _context.CurrentUser.SignIn(Carol);
        await _context.AppService.CreateChallengeAsync(new CreateChallengeDto());

        var waiting = await _context.AppService.GetListAsync(new FilterMatchDto { Status = MatchStatus.Waiting });

        Assert.Equal(MatchStatus.Waiting, Assert.Single(waiting.Items).Status);
    }

    [Fact]
    public async Task Enqueue_ShouldPairPlayersAndStartMatch_WhenRatingsAreClose()
    {
        // Arrange
        _context.CurrentUser.SignIn(Alice);
        var first = await _context.AppService.EnqueueAsync(new QueueRequestDto());
        _context.CurrentUser.SignIn(Bob);

        // Act
        var second = await _context.AppService.EnqueueAsync(new QueueRequestDto());

        // Assert
        Assert.Null(first.MatchId);
        Assert.NotNull(second.MatchId);
        Assert.Equal(0, _context.Queue.Count);
        var match = await _context.AppService.GetByIdAsync(new EntityIdDto<Guid> { Id = second.MatchId!.Value });
        Assert.Equal(MatchStatus.Active, match.Status);
        A.CallTo(() => _context.Notifier.MatchFoundAsync(Alice, A<MatchFoundDto>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Enqueue_ShouldKeepWaiting_WhenTimeControlsDiffer()
    {
        _context.CurrentUser.SignIn(Alice);
        await _context.AppService.EnqueueAsync(new QueueRequestDto { TimeControl = "4+2" });
        _context.CurrentUser.SignIn(Bob);

        var second = await _context.AppService.EnqueueAsync(new QueueRequestDto { TimeControl = "10+0" });

        Assert.Null(second.MatchId);
        Assert.Equal(2, _context.Queue.Count);
    }

    [Fact]
    public async Task LeaveQueue_ShouldRemoveTicket_WhenTicketBelongsToCaller()
    {
        _context.CurrentUser.SignIn(Alice);
        var ticket = await _context.AppService.EnqueueAsync(new QueueRequestDto());

        await _context.AppService.LeaveQueueAsync(new QueueTicketDto { TicketId = ticket.TicketId });

        Assert.Equal(0, _context.Queue.Count);
        await Assert.ThrowsAsync<NotFoundException>(() => _context.AppService.LeaveQueueAsync(new QueueTicketDto { TicketId = ticket.TicketId }));
    }

    [Fact]
    public async Task GetStats_ShouldAggregateFinishedMatches_WhenInRange()
    {
        // Arrange: one decisive game (North wins by capturing the Amir) and one resignation by South.
        var first = await _context.StartMatchAsync();
        for (var ply = 0; ply < ShortGame.Length; ply++)
        {
            await _context.PlayService.MakeMoveAsync(first.Id, first.PlayerAt(ply), ShortGame[ply], ply);
        }

        var second = await _context.StartMatchAsync();
        await _context.PlayService.ResignAsync(second.Id, second.SouthOf());
        await _context.StartMatchAsync(); // still active: not counted
        _context.CurrentUser.SignIn(Carol, "Carol", MatchesPermissions.ViewBalance);

        // Act
        var stats = await _context.AppService.GetStatsAsync(new MatchStatsRequestDto
        {
            From = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            To = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
            RulesVersion = "0.6",
        });

        // Assert
        Assert.Equal(2, stats.Games);
        Assert.Equal(2, stats.NorthWins);
        Assert.Equal(0, stats.SouthScore);
        Assert.Equal(11, stats.MeanPlies);
        Assert.Equal(["amirCaptured", "resign"], stats.EndReasons.Select(r => r.Reason).Order(StringComparer.Ordinal));
        var day = Assert.Single(stats.ByDay);
        Assert.Equal(new DateOnly(2026, 10, 10), day.Date);
    }

    [Fact]
    public async Task GetStats_ShouldThrowValidationException_WhenRangeIsReversed()
    {
        _context.CurrentUser.SignIn(Carol, "Carol", MatchesPermissions.ViewBalance);

        await Assert.ThrowsAsync<ValidationException>(() => _context.AppService.GetStatsAsync(new MatchStatsRequestDto
        {
            From = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc),
            To = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
        }));
    }

    [Theory]
    [InlineData(nameof(MatchesAppService.GetListAsync), MatchesPermissions.ViewMatch)]
    [InlineData(nameof(MatchesAppService.GetStatsAsync), MatchesPermissions.ViewBalance)]
    public void Endpoint_ShouldRequirePermission_WhenAdminOnly(string method, string permission)
    {
        var attribute = typeof(MatchesAppService).GetMethod(method)!.GetCustomAttribute<HasPermissionAttribute>();

        Assert.NotNull(attribute);
        Assert.Equal(permission, attribute.Permission);
    }

    [Fact]
    public void AppService_ShouldBeAuthorizedApiControllerWithExplicitRoute_WhenDeclared()
    {
        var type = typeof(MatchesAppService);

        Assert.True(typeof(ApplicationService).IsAssignableFrom(type));
        Assert.NotNull(type.GetCustomAttribute<AuthorizeAttribute>(inherit: true));
        Assert.NotNull(type.GetCustomAttribute<ApiControllerAttribute>(inherit: true));
        Assert.Equal("matches", type.GetCustomAttribute<RouteAttribute>()!.Template);
        Assert.NotNull(typeof(MatchHub).GetCustomAttribute<AuthorizeAttribute>());
    }

    [Fact]
    public async Task Match_ShouldBeSoftDeleted_WhenDeleted()
    {
        _context.CurrentUser.SignIn(Alice);
        var challenge = await _context.AppService.CreateChallengeAsync(new CreateChallengeDto());
        var repository = _context.Get<Qala.Framework.Domain.Repositories.IRepository<Match, Guid>>();

        await repository.DeleteAsync(await repository.GetAsync(challenge.MatchId), autoSave: true);

        Assert.Null(await repository.FindAsync(challenge.MatchId));
        var raw = _context.DbContext.Matches.IgnoreQueryFilters().Single(m => m.Id == challenge.MatchId);
        Assert.True(raw.IsDeleted);
        Assert.Equal(Alice, raw.DeleterId);
        Assert.Equal(Alice, raw.CreatorId);
    }
}
