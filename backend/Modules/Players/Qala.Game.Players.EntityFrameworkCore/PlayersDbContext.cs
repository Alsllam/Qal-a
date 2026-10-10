using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Qala.Framework.EntityFrameworkCore.Identity;
using Qala.Game.Players.Domain.Constants;
using Qala.Game.Players.Domain.Entities;

namespace Qala.Game.Players.EntityFrameworkCore;

/// <summary>The Players module's own DbContext, in schema <c>players</c>.</summary>
public class PlayersDbContext(DbContextOptions<PlayersDbContext> options) : DbContext(options)
{
    public DbSet<Player> Players => Set<Player>();

    public DbSet<RatedMatch> RatedMatches => Set<RatedMatch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(PlayerConsts.Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlayersDbContext).Assembly);
    }
}

/// <summary>Lets <c>dotnet ef</c> build the model without a host.</summary>
public sealed class PlayersDbContextFactory : IDesignTimeDbContextFactory<PlayersDbContext>
{
    public PlayersDbContext CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<PlayersDbContext>()
            .UseSqlServer(DesignTimeConnection.Get(PlayerConsts.ConnectionStringName), PlayersEntityFrameworkCoreModule.ConfigureSqlServer)
            .Options);
}

public static class PlayersEntityFrameworkCoreModule
{
    /// <summary>PlayersDbContext on SQL Server (<c>ConnectionStrings:Players</c>), aliased as the module's <see cref="DbContext"/>.</summary>
    public static IServiceCollection AddPlayersEntityFrameworkCoreModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(PlayerConsts.ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{PlayerConsts.ConnectionStringName}' is missing (set ConnectionStrings__{PlayerConsts.ConnectionStringName}).");
        services.AddDbContext<PlayersDbContext>(options => options.UseSqlServer(connectionString, ConfigureSqlServer));
        return services.AddPlayersRepositories();
    }

    /// <summary>The DbContext alias (tests call this after registering an InMemory context).</summary>
    public static IServiceCollection AddPlayersRepositories(this IServiceCollection services)
    {
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<PlayersDbContext>());
        return services;
    }

    public static void ConfigureSqlServer(SqlServerDbContextOptionsBuilder sql)
    {
        ArgumentNullException.ThrowIfNull(sql);
        sql.MigrationsHistoryTable("__EFMigrationsHistory", PlayerConsts.Schema);
        // No EnableRetryOnFailure: the rating consumer uses explicit transactions, which the retrying strategy forbids.
    }
}
