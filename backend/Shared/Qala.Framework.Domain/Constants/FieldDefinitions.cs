namespace Qala.Framework.Domain.Constants;

/// <summary>Shared field limits and patterns. Validators and EF configurations read them from here.</summary>
public static class FieldDefinitions
{
    public const int MaxNameLength = 120;
    public const int MinDisplayNameLength = 2;
    public const int MaxDisplayNameLength = 32;
    public const int MaxCodeLength = 32;
    public const int MaxEmailLength = 256;
    public const int MaxAvatarIdLength = 64;
    public const int MaxLocaleLength = 8;
    public const int MaxRulesVersionLength = 16;
    public const int MaxPositionLength = 96;
    public const int MaxMoveLength = 8;
    public const int MaxEndReasonLength = 32;
    public const int MaxPageSize = 100;
    public const int DefaultPageSize = 20;

    /// <summary>Letters (any script), digits, spaces, underscore, dot and hyphen.</summary>
    public const string DisplayNamePattern = @"^[\p{L}\p{Mn}\d _.\-]+$";

    /// <summary>Letters (any script), digits and spaces.</summary>
    public const string NamePattern = @"^[\p{L}\d ]+$";

    /// <summary>Supported UI languages.</summary>
    public static readonly IReadOnlyList<string> SupportedLocales = ["ar", "en"];

    /// <summary>Default sort for paged lists.</summary>
    public const string DefaultSorting = "CreationTime Desc";
}
