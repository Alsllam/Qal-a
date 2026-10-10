namespace Qala.Framework.Domain.DataSeeding;

/// <summary>Seeds reference data. Must be idempotent: check before inserting. Run by Qala.Game.DbMigrator.</summary>
public interface IDataSeeder
{
    /// <summary>Lower runs first.</summary>
    int Order => 0;

    Task SeedAsync(CancellationToken cancellationToken = default);
}
