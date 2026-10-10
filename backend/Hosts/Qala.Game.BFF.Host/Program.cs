using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Qala.Framework.Application.Extensions;
using Qala.Framework.Application.Options;
using Qala.Game.BFF.Host;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;
builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

services.AddCORSExtensions(configuration);
services.AddLoggingService(configuration);
services.AddSlidingWindowRateLimiterStrategy(configuration);
services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
services.AddReverseProxy().LoadFromConfig(configuration.GetSection("ReverseProxy"));
services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy());

var app = builder.Build();

app.UseLoggingMiddleware();
app.UseMiddleware<SecurityHeadersMiddleware>();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseResponseCompression();
app.UseCors(CorsSettings.PolicyName);
app.UseWebSockets();
app.UseRateLimiter();

app.MapHealthChecks("/health").AllowAnonymous();

// Authentication is not terminated here: every API validates the OpenIddict token itself.
app.MapReverseProxy().RequireRateLimiting(RateLimiterSettings.PolicyName);

await app.RunAsync();

/// <summary>Entry point; public so integration tests can host it.</summary>
public partial class Program;
