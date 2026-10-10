using Microsoft.Extensions.Diagnostics.HealthChecks;
using Qala.Framework.Application.Extensions;
using Qala.Framework.Application.Options;
using Qala.Framework.EntityFrameworkCore.Extensions;
using Qala.Game.Players.Application;
using Qala.Game.Players.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

services.AddPlayersApplicationModule(configuration);
services.AddControllers();
services.AddFluentValidationAutoValidationService();
services.AddCORSExtensions(configuration);
services.AddApiDefinition();
services.AddLocalizationService(typeof(PlayersApplicationModule).Assembly);
services.AddSwaggerService(configuration);
services.AddPlayersEntityFrameworkCoreModule(configuration);
services.AddSharedEntityFrameworkCoreModule(configuration, PlayersApplicationModule.ConsumersNamespace);
services.AddHttpContextAccessor();
services.AddLoggingService(configuration);
services.AddOpenIddictExtension(configuration);
services.ConfigureJsonOptions();
services.AddDynamicControllers(typeof(PlayersApplicationModule).Assembly);
services.SuppressModelStateInvalidFilter();
services.AddSlidingWindowRateLimiterStrategy(configuration);
services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy());

var app = builder.Build();

app.UseLocalizationMiddleware();
app.UseLoggingMiddleware();
app.UseExceptionHandlingMiddleware();
app.UseHttpsRedirection();
app.UseCors(CorsSettings.PolicyName);
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();
app.UseRateLimiter();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers().RequireRateLimiting(RateLimiterSettings.PolicyName);

await app.RunAsync();

/// <summary>Entry point; public so integration tests can host it.</summary>
public partial class Program;
