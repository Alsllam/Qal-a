using Qala.Framework.Domain.Events;
using Qala.Game.Matches.Application.Matches.DTOs;
using Qala.Game.Matches.Domain.Constants;
using Qala.Game.Matches.Domain.Enums;
using Qala.Game.Matches.Tests.Application.TestInfrastructure;
using static Qala.Game.Matches.Tests.Application.TestInfrastructure.MatchBuilder;

namespace Qala.Game.Matches.Tests.Application.Play;

public class MatchPlayServiceTests : IAsyncLifetime
{
    private readonly MatchesTestContext _context = new();

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await _context.DisposeAsync();

    [Fact]
    public async Task MakeMove_ShouldAcceptAndBroadcast_WhenMoveIsLegal()
    {
        // Arrange
        var match = await _context.StartMatchAsync();
        _context.Clock.Advance(TimeSpan.FromSeconds(10));

        // Act
        var result = await _context.PlayService.MakeMoveAsync(match.Id, match.SouthOf(), "d2-c3", 0);

        // Assert
        Assert.True(result.Status == MoveStatus.Accepted, result.ErrorKey);
        Assert.Equal("1fjajf1/2jrj2/7/7/2R4/2J1J2/1FJAJF1 n 1 0:0", result.MoveMade!.Position);
        Assert.Equal(240_000 - 10_000 + 2_000, result.MoveMade.Clocks.SouthMs);
        A.CallTo(() => _context.Notifier.MoveMadeAsync(A<MoveMadeDto>.That.Matches(m => m.Move == "d2-c3" && m.Ply == 0), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        var state = await _context.PlayService.GetStateAsync(match.Id, match.SouthOf(), canViewAny: false);
        Assert.Equal(1, state.Ply);
        Assert.Equal(["d2-c3"], state.Moves);
        Assert.Equal("north", state.ToMove);
    }

    [Theory]
    [InlineData("d1-d2")] // own piece on d2
    [InlineData("d2*d4")] // nothing to shoot
    [InlineData("b1-b5")] // Faris range is 3
    [InlineData("nonsense")]
    public async Task MakeMove_ShouldRejectAndKeepPosition_WhenMoveIsIllegal(string move)
    {
        var match = await _context.StartMatchAsync();

        var result = await _context.PlayService.MakeMoveAsync(match.Id, match.SouthOf(), move, 0);

        Assert.Equal(MoveStatus.Rejected, result.Status);
        Assert.Equal(MatchErrors.IllegalMove, result.ErrorKey);
        var state = await _context.PlayService.GetStateAsync(match.Id, match.SouthOf(), canViewAny: false);
        Assert.Equal(0, state.Ply);
        Assert.Equal(match.Position, state.Position);
        A.CallTo(() => _context.Notifier.MoveMadeAsync(A<MoveMadeDto>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task MakeMove_ShouldReject_WhenItIsNotTheCallersTurn()
    {
        var match = await _context.StartMatchAsync();

        var result = await _context.PlayService.MakeMoveAsync(match.Id, match.NorthOf(), "c6-c5", 0);

        Assert.Equal(MatchErrors.NotYourTurn, result.ErrorKey);
    }

    [Fact]
    public async Task MakeMove_ShouldReject_WhenCallerIsNotAPlayer()
    {
        var match = await _context.StartMatchAsync();

        var result = await _context.PlayService.MakeMoveAsync(match.Id, Carol, "d2-c3", 0);

        Assert.Equal(MatchErrors.NotParticipant, result.ErrorKey);
    }

    [Fact]
    public async Task MakeMove_ShouldIgnoreRetry_WhenSameMoveIsResentForAnOldPly()
    {
        // Arrange
        var match = await _context.StartMatchAsync();
        await _context.PlayService.MakeMoveAsync(match.Id, match.SouthOf(), "d2-c3", 0);
        await _context.NewScopeAsync();

        // Act: the client did not see the ack and resends the same move with the same ply.
        var retry = await _context.PlayService.MakeMoveAsync(match.Id, match.SouthOf(), "d2-c3", 0);

        // Assert
        Assert.Equal(MoveStatus.Duplicate, retry.Status);
        var state = await _context.PlayService.GetStateAsync(match.Id, match.SouthOf(), canViewAny: false);
        Assert.Equal(1, state.Ply);
        A.CallTo(() => _context.Notifier.MoveMadeAsync(A<MoveMadeDto>._, A<CancellationToken>._)).MustHaveHappenedOnceExactly();
    }

    [Theory]
    [InlineData(0, "e2-e3")] // an old ply with a different move
    [InlineData(5, "c6-c5")] // a ply from the future
    public async Task MakeMove_ShouldRejectStalePly_WhenPlyDoesNotMatchServer(int ply, string move)
    {
        var match = await _context.StartMatchAsync();
        await _context.PlayService.MakeMoveAsync(match.Id, match.SouthOf(), "d2-c3", 0);

        var result = await _context.PlayService.MakeMoveAsync(match.Id, match.NorthOf(), move, ply);

        Assert.Equal(MoveStatus.Rejected, result.Status);
        Assert.Equal(MatchErrors.StalePly, result.ErrorKey);
    }

    [Fact]
    public async Task MakeMove_ShouldFinishAndPublishEvent_WhenMoveCapturesTheAmir()
    {
        // Arrange
        var match = await _context.StartMatchAsync();
        MakeMoveResultDto? last = null;

        // Act: replay a real 22-ply game ending with c5xc2 (North takes the South Amir).
        for (var ply = 0; ply < ShortGame.Length; ply++)
        {
            _context.Clock.Advance(TimeSpan.FromSeconds(1));
            last = await _context.PlayService.MakeMoveAsync(match.Id, match.PlayerAt(ply), ShortGame[ply], ply);
            Assert.Equal(MoveStatus.Accepted, last.Status);
        }

        // Assert
        Assert.Equal(new MatchOutcomeDto("north", "amirCaptured"), last!.MoveMade!.Outcome);
        A.CallTo(() => _context.EventPublisher.PublishAsync(
                A<MatchFinishedEto>.That.Matches(e =>
                    e.MatchId == match.Id
                    && e.Winner == "north"
                    && e.Reason == "amirCaptured"
                    && e.Plies == 22
                    && e.RulesVersion == "0.6"
                    && e.SouthPlayerId == match.SouthOf()
                    && e.DurationMs == 22_000),
                A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        A.CallTo(() => _context.Notifier.MatchEndedAsync(A<MatchEndedDto>.That.Matches(e => e.Outcome!.Reason == "amirCaptured"), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();

        var after = await _context.PlayService.MakeMoveAsync(match.Id, match.SouthOf(), "a1-a2", 22);
        Assert.Equal(MatchErrors.NotActive, after.ErrorKey);
    }

    [Fact]
    public async Task Resign_ShouldFinishWithOpponentWinningAndPublishEvent_WhenPlayerResigns()
    {
        var match = await _context.StartMatchAsync();

        var resigned = await _context.PlayService.ResignAsync(match.Id, match.NorthOf());

        Assert.True(resigned);
        var state = await _context.PlayService.GetStateAsync(match.Id, match.NorthOf(), canViewAny: false);
        Assert.Equal(MatchStatus.Finished, state.Status);
        Assert.Equal(new MatchOutcomeDto("south", "resign"), state.Outcome);
        A.CallTo(() => _context.EventPublisher.PublishAsync(A<MatchFinishedEto>.That.Matches(e => e.Reason == "resign" && e.Winner == "south"), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ExpireClocks_ShouldEndGameOnTime_WhenSideToMoveRunsOut()
    {
        // Arrange: South moves, then North thinks for more than 4 minutes.
        var match = await _context.StartMatchAsync();
        await _context.PlayService.MakeMoveAsync(match.Id, match.SouthOf(), "d2-c3", 0);
        _context.Clock.Advance(TimeSpan.FromMinutes(4).Add(TimeSpan.FromSeconds(1)));
        await _context.NewScopeAsync();

        // Act
        var ended = await _context.PlayService.ExpireClocksAsync();

        // Assert
        Assert.Equal(1, ended);
        var state = await _context.PlayService.GetStateAsync(match.Id, match.SouthOf(), canViewAny: false);
        Assert.Equal(new MatchOutcomeDto("south", "timeout"), state.Outcome);
        Assert.Equal(0, state.Clocks.NorthMs);
        A.CallTo(() => _context.EventPublisher.PublishAsync(A<MatchFinishedEto>.That.Matches(e => e.Reason == "timeout"), A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ExpireClocks_ShouldDoNothing_WhenTimeRemains()
    {
        await _context.StartMatchAsync();
        _context.Clock.Advance(TimeSpan.FromMinutes(3));

        Assert.Equal(0, await _context.PlayService.ExpireClocksAsync());
        A.CallTo(() => _context.EventPublisher.PublishAsync(A<MatchFinishedEto>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task MakeMove_ShouldLoseOnTime_WhenMoveArrivesAfterTheFlag()
    {
        var match = await _context.StartMatchAsync("1+0");
        _context.Clock.Advance(TimeSpan.FromSeconds(61));

        var result = await _context.PlayService.MakeMoveAsync(match.Id, match.SouthOf(), "d2-c3", 0);

        Assert.Equal(MatchErrors.Timeout, result.ErrorKey);
        var state = await _context.PlayService.GetStateAsync(match.Id, match.SouthOf(), canViewAny: false);
        Assert.Equal(new MatchOutcomeDto("north", "timeout"), state.Outcome);
        Assert.Equal(0, state.Ply);
    }
}
