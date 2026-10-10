using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Qala.Framework.Domain.DataSeeding;
using Qala.Framework.Domain.Identity;
using Qala.Framework.Domain.Security;
using Qala.Framework.EntityFrameworkCore.Identity;
using Qala.Game.DbMigrator.DataSeeding;
using Qala.Game.Matches.Domain.Constants;
using Qala.Game.Matches.EntityFrameworkCore;
using Qala.Game.Players.Domain.Constants;
using Qala.Game.Players.EntityFrameworkCore;

// Applies the migrations of every DbContext (each module has its own schema), then runs the seeders in order.
// Hosts never migrate at startup.
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { Args = args, ContentRootPath = AppContext.BaseDirectory });
builder.Configuration.AddUserSecrets(typeof(PermissionsDataSeeder).Assembly, optional: true);
var configuration = builder.Configuration;
var services = builder.Services;

string Connection(string name) => configuration.GetConnectionString(name) is { Length: > 0 } value
    ? value
    : throw new InvalidOperationException($"Connection string '{name}' is missing (set ConnectionStrings__{name}).");

services.AddDbContext<QalaIdentityDbContext>(o =>
{
    o.UseSqlServer(Connection(QalaIdentityDbContext.ConnectionStringName), sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", QalaIdentityDbContext.Schema));
    o.UseOpenIddict<Guid>();
});
services.AddDbContext<MatchesDbContext>(o => o.UseSqlServer(Connection(MatchConsts.ConnectionStringName), MatchesEntityFrameworkCoreModule.ConfigureSqlServer));
services.AddDbContext<PlayersDbContext>(o => o.UseSqlServer(Connection(PlayerConsts.ConnectionStringName), PlayersEntityFrameworkCoreModule.ConfigureSqlServer));

services.AddIdentityCore<AppUser>()
    .AddRoles<AppRole>()
    .AddEntityFrameworkStores<QalaIdentityDbContext>();
services.AddOpenIddict().AddCore(o => o.UseEntityFrameworkCore().UseDbContext<QalaIdentityDbContext>().ReplaceDefaultEntities<Guid>());

services.AddSingleton(TimeProvider.System);
services.Configure<SeedSettings>(configuration.GetSection(SeedSettings.SectionName));
services.AddSingleton<IPermissionDefinitionProvider, MatchesPermissionDefinitionProvider>();
services.AddSingleton<IPermissionDefinitionProvider, PlayersPermissionDefinitionProvider>();
services.AddScoped<IDataSeeder, PermissionsDataSeeder>();
services.AddScoped<IDataSeeder, OpenIddictDataSeeder>();

using var host = builder.Build();
var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Qala.Game.DbMigrator");
using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(10));

try
{
    await using var scope = host.Services.CreateAsyncScope();
    DbContext[] contexts =
    [
        scope.ServiceProvider.GetRequiredService<QalaIdentityDbContext>(),
        scope.ServiceProvider.GetRequiredService<MatchesDbContext>(),
        scope.ServiceProvider.GetRequiredService<PlayersDbContext>(),
    ];
    foreach (var context in contexts)
    {
        var pending = (await context.Database.GetPendingMigrationsAsync(cancellation.Token)).ToList();
        logger.LogInformation("{Context}: applying {Count} migrations", context.GetType().Name, pending.Count);
        await context.Database.MigrateAsync(cancellation.Token);
    }

    foreach (var seeder in scope.ServiceProvider.GetServices<IDataSeeder>().OrderBy(s => s.Order))
    {
        logger.LogInformation("Running {Seeder}", seeder.GetType().Name);
        await seeder.SeedAsync(cancellation.Token);
    }

    logger.LogInformation("Database is up to date");
    return 0;
}
catch (Exception exception)
{
    logger.LogCritical(exception, "Migration failed");
    return 1;
}
