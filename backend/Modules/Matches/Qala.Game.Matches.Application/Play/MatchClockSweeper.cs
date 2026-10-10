using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Qala.Game.Matches.Application.Play;

/// <summary><c>Matches:Clock</c> section.</summary>
public sealed class MatchClockSettings
{
    public const string SectionName = "Matches:Clock";

    public double SweepIntervalSeconds { get; set; } = 1;
}

/// <summary>
/// Ends games on time when nobody moves. Runs in every Matches host instance; concurrent sweeps are safe because
/// matches carry a concurrency stamp. TODO: end games after 60 s of disconnection (track hub connections).
/// </summary>
public sealed partial class MatchClockSweeper(IServiceScopeFactory scopeFactory, IOptions<MatchClockSettings> settings, ILogger<MatchClockSweeper> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(0.2, settings.Value.SweepIntervalSeconds)));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var ended = await scope.ServiceProvider.GetRequiredService<IMatchPlayService>().ExpireClocksAsync(stoppingToken);
                if (ended > 0)
                {
                    LogTimeouts(logger, ended);
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogSweepFailed(logger, exception);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{Count} matches ended on time")]
    private static partial void LogTimeouts(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "Clock sweep failed")]
    private static partial void LogSweepFailed(ILogger logger, Exception exception);
}
