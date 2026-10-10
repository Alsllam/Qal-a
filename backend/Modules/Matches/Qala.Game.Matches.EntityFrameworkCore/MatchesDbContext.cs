using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Qala.Framework.EntityFrameworkCore.Identity;
using Qala.Game.Matches.Domain.Constants;
using Qala.Game.Matches.Domain.Entities;

namespace Qala.Game.Matches.EntityFrameworkCore;

/// <summary>The Matches module's own DbContext, in schema <c>matches</c>.</summary>
public class MatchesDbContext(DbContextOptions<MatchesDbContext> options) : DbContext(options)
{
    public DbSet<Match> Matches => Set<Match>();

    public DbSet<MatchMove> MatchMoves => Set<MatchMove>();

    public DbSet<PlayerRating> PlayerRatings => Set<PlayerRating>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);

        // Moves are only ever loaded through their (soft-deletable) match, so the filter interaction is intended.
        optionsBuilder.ConfigureWarnings(w => w.Ignore(CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.HasDefaultSchema(MatchConsts.Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MatchesDbContext).Assembly);
    }
}

/// <summary>Lets <c>dotnet ef</c> build the model without a host.</summary>
public sealed class MatchesDbContextFactory : IDesignTimeDbContextFactory<MatchesDbContext>
{
    public MatchesDbContext CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<MatchesDbContext>()
            .UseSqlServer(DesignTimeConnection.Get(MatchConsts.ConnectionStringName), MatchesEntityFrameworkCoreModule.ConfigureSqlServer)
            .Options);
}
