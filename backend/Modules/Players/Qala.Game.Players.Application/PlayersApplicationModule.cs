using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Qala.Game.Players.Application.Players;

namespace Qala.Game.Players.Application;

public static class PlayersApplicationModule
{
    /// <summary>Consumers namespace passed to <c>AddSharedEntityFrameworkCoreModule</c>.</summary>
    public const string ConsumersNamespace = "Qala.Game.Players.Application.EventHandlers";

    public static IServiceCollection AddPlayersApplicationModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IPlayersAppService, PlayersAppService>();
        services.AddValidatorsFromAssembly(typeof(PlayersApplicationModule).Assembly);
        return services;
    }
}
