using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Qala.Framework.Application.Localization;
using Qala.Framework.Domain.Events;
using Qala.Framework.Domain.Security;
using Qala.Framework.EntityFrameworkCore.Extensions;
using Qala.Game.Matches.Application;
using Qala.Game.Matches.Application.Matches;
using Qala.Game.Matches.Application.Matchmaking;
using Qala.Game.Matches.Application.Play;
using Qala.Game.Matches.EntityFrameworkCore;

namespace Qala.Game.Matches.Tests.Application.TestInfrastructure;

/// <summary>
/// The Matches application layer wired like a host, but on EF Core InMemory with a manual clock, a fake notifier and
/// a fake event publisher. Each test gets its own database.
/// </summary>
public sealed class MatchesTestContext : IAsyncDisposable
{
    private readonly ServiceProvider _provider;
    private int _codeCounter;

    public MatchesTestContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var databaseName = $"matches-{Guid.NewGuid():N}";
        services.AddDbContext<MatchesDbContext>(o => o.UseInMemoryDatabase(databaseName));
        services.AddMatchesRepositories();
        services.AddSharedRepositories();
        services.AddSingleton<TimeProvider>(Clock);
        services.AddSingleton<ICurrentUser>(CurrentUser);
        services.AddSingleton(EventPublisher);
        services.AddSingleton(Notifier);
        services.AddSingleton<IMatchmakingQueue>(Queue);
        services.AddSingleton(CodeGenerator);
        services.AddSingleton(new JsonLocalizationStore([typeof(LocalizationKeys).Assembly, typeof(MatchesApplicationModule).Assembly]));
        services.AddSingleton<IStringLocalizer, JsonStringLocalizer>();
        services.AddValidatorsFromAssembly(typeof(MatchesApplicationModule).Assembly);
        services.AddScoped<MatchesAppService>();
        services.AddScoped<IMatchPlayService, MatchPlayService>();
        _provider = services.BuildServiceProvider();
        Scope = _provider.CreateAsyncScope();

        A.CallTo(() => CodeGenerator.NewCode()).ReturnsLazily(() => $"ABC{Interlocked.Increment(ref _codeCounter):D3}".Replace('0', 'Z').Replace('1', 'Y'));
    }

    public ManualTimeProvider Clock { get; } = new(new DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero));

    public TestCurrentUser CurrentUser { get; } = new();

    public IEventPublisher EventPublisher { get; } = A.Fake<IEventPublisher>();

    public IMatchNotifier Notifier { get; } = A.Fake<IMatchNotifier>();

    public InMemoryMatchmakingQueue Queue { get; } = new();

    public IChallengeCodeGenerator CodeGenerator { get; } = A.Fake<IChallengeCodeGenerator>();

    public AsyncServiceScope Scope { get; private set; }

    public MatchesAppService AppService => Scope.ServiceProvider.GetRequiredService<MatchesAppService>();

    public IMatchPlayService PlayService => Scope.ServiceProvider.GetRequiredService<IMatchPlayService>();

    public MatchesDbContext DbContext => Scope.ServiceProvider.GetRequiredService<MatchesDbContext>();

    public T Get<T>()
        where T : notnull => Scope.ServiceProvider.GetRequiredService<T>();

    /// <summary>A fresh scope (new DbContext), like a new request.</summary>
    public async Task NewScopeAsync()
    {
        await Scope.DisposeAsync();
        Scope = _provider.CreateAsyncScope();
    }

    public async ValueTask DisposeAsync()
    {
        await Scope.DisposeAsync();
        await _provider.DisposeAsync();
    }
}
