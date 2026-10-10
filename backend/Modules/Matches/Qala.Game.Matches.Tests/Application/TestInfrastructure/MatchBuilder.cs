using Qala.Game.Matches.Application.Matches.DTOs;

namespace Qala.Game.Matches.Tests.Application.TestInfrastructure;

public static class MatchBuilder
{
    public static readonly Guid Alice = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    public static readonly Guid Bob = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
    public static readonly Guid Carol = Guid.Parse("cccccccc-0000-0000-0000-000000000003");

    /// <summary>A real 22-ply game from the rules test vectors: North captures the South Amir with c5xc2.</summary>
    public static readonly string[] ShortGame =
        "d2-e3 b7-a7 f1-f2 f7-f5 e3-d4 a7-a4 d4-e5 a4-a5 c2-b2 f5xe5 e1-f1 a5-a6 f2-f5 e5-c5 d1-c2 e7-f7 b2-a2 a6-a3 e2-d2 a3-a6 b1-a1 c5xc2"
            .Split(' ');

    /// <summary>Alice challenges, Bob accepts. Returns the active match.</summary>
    public static async Task<MatchDto> StartMatchAsync(this MatchesTestContext context, string timeControl = "4+2")
    {
        context.CurrentUser.SignIn(Alice, "Alice");
        var challenge = await context.AppService.CreateChallengeAsync(new CreateChallengeDto { TimeControl = timeControl });
        context.CurrentUser.SignIn(Bob, "Bob");
        var match = await context.AppService.AcceptChallengeAsync(new AcceptChallengeDto { Code = challenge.Code });
        await context.NewScopeAsync();
        return match;
    }

    public static Guid SouthOf(this MatchDto match) => match.South!.PlayerId;

    public static Guid NorthOf(this MatchDto match) => match.North!.PlayerId;

    /// <summary>The player whose turn it is at <paramref name="ply"/>.</summary>
    public static Guid PlayerAt(this MatchDto match, int ply) => ply % 2 == 0 ? match.SouthOf() : match.NorthOf();
}
