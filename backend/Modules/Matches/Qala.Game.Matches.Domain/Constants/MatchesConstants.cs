using Qala.Framework.Domain.Security;

namespace Qala.Game.Matches.Domain.Constants;

/// <summary>Permission names of the Matches module (docs/architecture.md §4).</summary>
public static class MatchesPermissions
{
    public const string ViewMatch = "Permissions.Matches.ViewMatch";

    /// <summary>The live balance dashboard (<c>POST stats</c>).</summary>
    public const string ViewBalance = "Permissions.Dashboard.ViewBalance";

    public static IReadOnlyList<string> All { get; } = [ViewMatch, ViewBalance];
}

public sealed class MatchesPermissionDefinitionProvider : IPermissionDefinitionProvider
{
    public IEnumerable<string> GetPermissions() => MatchesPermissions.All;
}

public static class MatchConsts
{
    public const string Schema = "matches";
    public const string ConnectionStringName = "Matches";
    public const int ChallengeCodeLength = 6;

    /// <summary>No 0/O, 1/I: codes are read aloud and typed on phones.</summary>
    public const string ChallengeCodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public const string ChallengeCodePattern = "^[A-HJ-NP-Z2-9]{6}$";

    /// <summary><c>minutes+seconds</c>, e.g. <c>4+2</c>.</summary>
    public const string TimeControlPattern = @"^\d{1,2}\+\d{1,2}$";

    public const string DefaultTimeControl = "4+2";

    /// <summary>Longest stats range in days.</summary>
    public const int MaxStatsRangeDays = 366;
}

/// <summary>Localization keys of the Matches module (Resources/{ar,en}.json).</summary>
public static class MatchErrors
{
    public const string NotActive = "Matches:Errors:NotActive";
    public const string NotParticipant = "Matches:Errors:NotParticipant";
    public const string NotYourTurn = "Matches:Errors:NotYourTurn";
    public const string StalePly = "Matches:Errors:StalePly";
    public const string IllegalMove = "Matches:Errors:IllegalMove";
    public const string Timeout = "Matches:Errors:Timeout";
    public const string ChallengeNotFound = "Matches:Errors:ChallengeNotFound";
    public const string OwnChallenge = "Matches:Errors:OwnChallenge";
    public const string InvalidTimeControl = "Matches:Errors:InvalidTimeControl";
    public const string InvalidChallengeCode = "Matches:Errors:InvalidChallengeCode";
    public const string InvalidStatsRange = "Matches:Errors:InvalidStatsRange";
    public const string TicketNotFound = "Matches:Errors:TicketNotFound";
    public const string Conflict = "Matches:Errors:Conflict";
}
