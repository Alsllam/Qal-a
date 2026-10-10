using Qala.Framework.Domain.Security;

namespace Qala.Game.Players.Domain.Constants;

/// <summary>Permission names of the Players module (docs/architecture.md §4).</summary>
public static class PlayersPermissions
{
    public const string ViewPlayer = "Permissions.Players.ViewPlayer";

    /// <summary>Ban (deactivate) and unban (activate) players.</summary>
    public const string ManagePlayer = "Permissions.Players.ManagePlayer";

    public static IReadOnlyList<string> All { get; } = [ViewPlayer, ManagePlayer];
}

public sealed class PlayersPermissionDefinitionProvider : IPermissionDefinitionProvider
{
    public IEnumerable<string> GetPermissions() => PlayersPermissions.All;
}

public static class PlayerConsts
{
    public const string Schema = "players";
    public const string ConnectionStringName = "Players";
    public const string DefaultLocale = "ar";
}

/// <summary>Localization keys of the Players module (Resources/{ar,en}.json).</summary>
public static class PlayerErrors
{
    public const string ProfileNotFound = "Players:Errors:ProfileNotFound";
    public const string Banned = "Players:Errors:Banned";
    public const string UnsupportedLocale = "Players:Errors:UnsupportedLocale";
}
