using FluentValidation;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Qala.Game.Matches.Application.Matches;
using Qala.Game.Matches.Application.Matchmaking;
using Qala.Game.Matches.Application.Play;

namespace Qala.Game.Matches.Application;

public static class MatchesApplicationModule
{
    /// <summary>Consumers namespace passed to <c>AddSharedEntityFrameworkCoreModule</c>.</summary>
    public const string ConsumersNamespace = "Qala.Game.Matches.Application.EventHandlers";

    /// <summary>AppServices, validators, the play service, matchmaking, SignalR and the clock sweeper.</summary>
    public static IServiceCollection AddMatchesApplicationModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IMatchesAppService, MatchesAppService>();
        services.AddScoped<IMatchPlayService, MatchPlayService>();
        services.AddScoped<IMatchNotifier, SignalRMatchNotifier>();
        services.AddSingleton<IChallengeCodeGenerator, RandomChallengeCodeGenerator>();
        services.AddSingleton<IMatchmakingQueue, InMemoryMatchmakingQueue>();
        services.AddValidatorsFromAssembly(typeof(MatchesApplicationModule).Assembly, includeInternalTypes: false);

        services.AddSignalR();
        services.AddSingleton<IUserIdProvider, SubClaimUserIdProvider>();
        services.Configure<MatchClockSettings>(configuration.GetSection(MatchClockSettings.SectionName));
        services.AddHostedService<MatchClockSweeper>();
        return services;
    }
}
