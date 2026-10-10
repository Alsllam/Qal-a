using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Qala.Framework.Domain.Identity;

namespace Qala.Framework.EntityFrameworkCore.Identity;

/// <summary>Accounts, roles (with permission claims) and the OpenIddict tables, in schema <c>auth</c>.</summary>
public class QalaIdentityDbContext(DbContextOptions<QalaIdentityDbContext> options)
    : IdentityDbContext<AppUser, AppRole, Guid>(options)
{
    public const string Schema = "auth";
    public const string ConnectionStringName = "Auth";

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema(Schema);
        builder.UseOpenIddict<Guid>();
        builder.Entity<AppUser>(b => b.Property(u => u.DisplayName).HasMaxLength(Domain.Constants.FieldDefinitions.MaxDisplayNameLength));
    }
}

/// <summary>Lets <c>dotnet ef</c> build the model without a host. The connection string is only used when applying.</summary>
public sealed class QalaIdentityDbContextFactory : IDesignTimeDbContextFactory<QalaIdentityDbContext>
{
    public QalaIdentityDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<QalaIdentityDbContext>()
            .UseSqlServer(DesignTimeConnection.Get(ConnectionStringNameForDesign), sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", QalaIdentityDbContext.Schema))
            .Options;
        return new QalaIdentityDbContext(options);
    }

    private const string ConnectionStringNameForDesign = QalaIdentityDbContext.ConnectionStringName;
}

/// <summary>Design-time connection strings: <c>ConnectionStrings__{name}</c> from the environment, else local dev SQL Server.</summary>
public static class DesignTimeConnection
{
    public static string Get(string name) =>
        Environment.GetEnvironmentVariable($"ConnectionStrings__{name}")
        ?? "Server=localhost,1433;Database=QalaGame;User Id=sa;Password=placeholder;TrustServerCertificate=True";
}
