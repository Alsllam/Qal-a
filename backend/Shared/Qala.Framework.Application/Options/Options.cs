namespace Qala.Framework.Application.Options;

/// <summary><c>Cors</c> section. Origins are explicit; credentials are allowed for SignalR.</summary>
public sealed class CorsSettings
{
    public const string SectionName = "Cors";
    public const string PolicyName = "Default";

    public string[] AllowedOrigins { get; set; } = [];
}

/// <summary><c>RateLimiter</c> section for the <c>SlidingPolicy</c>.</summary>
public sealed class RateLimiterSettings
{
    public const string SectionName = "RateLimiter";
    public const string PolicyName = "SlidingPolicy";

    /// <summary>Window length in seconds.</summary>
    public int Window { get; set; } = 60;

    public int PermitLimit { get; set; } = 120;

    public int SegmentsPerWindow { get; set; } = 6;
}

/// <summary><c>Auth</c> section: where tokens come from and which audience this API is.</summary>
public sealed class AuthSettings
{
    public const string SectionName = "Auth";

    /// <summary>The OpenIddict server (Qala.Game.Auth.Host), e.g. <c>http://localhost:5001/</c>.</summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>This API's audience / resource name, e.g. <c>matches-api</c>.</summary>
    public string Audience { get; set; } = string.Empty;
}

/// <summary><c>Swagger</c> section.</summary>
public sealed class SwaggerSettings
{
    public const string SectionName = "Swagger";

    public string Title { get; set; } = "Qal'a API";

    public string Version { get; set; } = "v1";
}
