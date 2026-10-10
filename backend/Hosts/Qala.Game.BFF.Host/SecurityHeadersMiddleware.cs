namespace Qala.Game.BFF.Host;

/// <summary>Adds browser security headers to every response that does not set its own.</summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    /// <summary>APIs return JSON only; the Auth host's login page sets its own policy.</summary>
    public const string DefaultContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";

    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.TryAdd("X-Content-Type-Options", "nosniff");
            headers.TryAdd("X-Frame-Options", "DENY");
            headers.TryAdd("Referrer-Policy", "no-referrer");
            headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
            headers.TryAdd("Cross-Origin-Opener-Policy", "same-origin");
            headers.TryAdd("Content-Security-Policy", DefaultContentSecurityPolicy);
            headers.Remove("Server");
            headers.Remove("X-Powered-By");
            return Task.CompletedTask;
        });
        return next(context);
    }
}
