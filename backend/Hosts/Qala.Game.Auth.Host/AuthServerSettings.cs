namespace Qala.Game.Auth.Host;

/// <summary><c>AuthServer</c> section.</summary>
public sealed class AuthServerSettings
{
    public const string SectionName = "AuthServer";

    /// <summary>The public issuer: the BFF address, since clients reach <c>/connect/**</c> through it.</summary>
    public string Issuer { get; set; } = "http://localhost:5000/";

    /// <summary>Production only: PFX files and their passwords (from environment variables or a vault).</summary>
    public string? SigningCertificatePath { get; set; }

    public string? SigningCertificatePassword { get; set; }

    public string? EncryptionCertificatePath { get; set; }

    public string? EncryptionCertificatePassword { get; set; }

    public int AccessTokenLifetimeMinutes { get; set; } = 30;

    public int RefreshTokenLifetimeDays { get; set; } = 30;
}

/// <summary>The API scopes (each scope's resource has the same name, used as the token audience).</summary>
public static class ApiScopes
{
    public const string Players = "players-api";
    public const string Matches = "matches-api";
    public const string Ai = "ai-api";

    public static IReadOnlyList<string> All { get; } = [Players, Matches, Ai];
}
