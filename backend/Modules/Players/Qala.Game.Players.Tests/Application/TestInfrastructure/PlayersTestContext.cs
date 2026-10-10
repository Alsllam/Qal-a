using System.Security.Claims;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Qala.Framework.Application.Localization;
using Qala.Framework.Domain.Events;
using Qala.Framework.Domain.Security;
using Qala.Framework.EntityFrameworkCore.Extensions;
using Qala.Game.Players.Application;
using Qala.Game.Players.Application.Players;
using Qala.Game.Players.EntityFrameworkCore;

namespace Qala.Game.Players.Tests.Application.TestInfrastructure;

public sealed class TestCurrentUser : ICurrentUser
{
    public Guid? Id { get; set; }

    public string? UserName { get; set; }

    public HashSet<string> Permissions { get; } = [];

    public bool IsAuthenticated => Id is not null;

    public ClaimsPrincipal? Principal => null;

    public bool HasPermission(string permission) => Permissions.Contains(permission);

    public void SignIn(Guid userId, string? name = null, params string[] permissions)
    {
        Id = userId;
        UserName = name;
        Permissions.Clear();
        Permissions.UnionWith(permissions);
    }
}

/// <summary>The Players application layer on EF Core InMemory with a fake event publisher.</summary>
public sealed class PlayersTestContext : IAsyncDisposable
{
    private readonly ServiceProvider _provider;

    public PlayersTestContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var databaseName = $"players-{Guid.NewGuid():N}";
        services.AddDbContext<PlayersDbContext>(o => o.UseInMemoryDatabase(databaseName));
        services.AddPlayersRepositories();
        services.AddSharedRepositories();
        services.AddSingleton<ICurrentUser>(CurrentUser);
        services.AddSingleton(EventPublisher);
        services.AddSingleton(new JsonLocalizationStore([typeof(LocalizationKeys).Assembly, typeof(PlayersApplicationModule).Assembly]));
        services.AddSingleton<IStringLocalizer, JsonStringLocalizer>();
        services.AddValidatorsFromAssembly(typeof(PlayersApplicationModule).Assembly);
        services.AddScoped<PlayersAppService>();
        _provider = services.BuildServiceProvider();
        Scope = _provider.CreateAsyncScope();
    }

    public TestCurrentUser CurrentUser { get; } = new();

    public IEventPublisher EventPublisher { get; } = A.Fake<IEventPublisher>();

    public AsyncServiceScope Scope { get; private set; }

    public PlayersAppService AppService => Scope.ServiceProvider.GetRequiredService<PlayersAppService>();

    public PlayersDbContext DbContext => Scope.ServiceProvider.GetRequiredService<PlayersDbContext>();

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
