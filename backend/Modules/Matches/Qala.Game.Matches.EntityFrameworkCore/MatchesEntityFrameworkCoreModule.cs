using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Qala.Game.Matches.Domain.Constants;
using Qala.Game.Matches.Domain.Repositories;
using Qala.Game.Matches.EntityFrameworkCore.Repositories;

namespace Qala.Game.Matches.EntityFrameworkCore;

public static class MatchesEntityFrameworkCoreModule
{
    /// <summary>MatchesDbContext on SQL Server (<c>ConnectionStrings:Matches</c>), aliased as the module's <see cref="DbContext"/>.</summary>
    public static IServiceCollection AddMatchesEntityFrameworkCoreModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(MatchConsts.ConnectionStringName)
            ?? throw new InvalidOperationException($"Connection string '{MatchConsts.ConnectionStringName}' is missing (set ConnectionStrings__{MatchConsts.ConnectionStringName}).");
        services.AddDbContext<MatchesDbContext>(options => options.UseSqlServer(connectionString, ConfigureSqlServer));
        return services.AddMatchesRepositories();
    }

    /// <summary>The DbContext alias and the custom repositories (tests call this after registering an InMemory context).</summary>
    public static IServiceCollection AddMatchesRepositories(this IServiceCollection services)
    {
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<MatchesDbContext>());
        services.AddScoped<IMatchReadOnlyRepository, MatchReadOnlyRepository>();
        return services;
    }

    public static void ConfigureSqlServer(SqlServerDbContextOptionsBuilder sql)
    {
        ArgumentNullException.ThrowIfNull(sql);
        sql.MigrationsHistoryTable("__EFMigrationsHistory", MatchConsts.Schema);
        sql.EnableRetryOnFailure(3);
    }
}
