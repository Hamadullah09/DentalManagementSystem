using DentalSurgery.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DentalSurgery.Infrastructure.Persistence;

/// <summary>
/// Used by <c>dotnet ef</c> at design time. The runtime application builds its
/// own context from configuration; this exists only so migrations can be
/// scaffolded without starting the web host.
/// <para>
/// The provider is chosen by the <c>DENTAL_MIGRATION_PROVIDER</c> environment
/// variable, because the two providers keep their migrations in separate
/// assemblies and each must be scaffolded against its own dialect. No database
/// needs to exist: EF only needs a configured provider to generate the SQL.
/// </para>
/// <example>
/// <code>
/// # SQL Server (production)
/// DENTAL_MIGRATION_PROVIDER=SqlServer dotnet ef migrations add Name \
///   --project src/DentalSurgery.Migrations.SqlServer \
///   --startup-project src/DentalSurgery.Web
///
/// # SQLite (local development)
/// DENTAL_MIGRATION_PROVIDER=Sqlite dotnet ef migrations add Name \
///   --project src/DentalSurgery.Migrations.Sqlite \
///   --startup-project src/DentalSurgery.Web
/// </code>
/// </example>
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DentalDbContext>
{
    public DentalDbContext CreateDbContext(string[] args)
    {
        var provider = Environment.GetEnvironmentVariable("DENTAL_MIGRATION_PROVIDER");
        var useSqlite = string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase);

        var connection = args.FirstOrDefault(a => a.StartsWith("cs=", StringComparison.OrdinalIgnoreCase))?[3..];

        var builder = new DbContextOptionsBuilder<DentalDbContext>()
            .EnableSensitiveDataLogging(false);

        if (useSqlite)
        {
            builder.UseSqlite(
                connection ?? "Data Source=dental-design.db",
                sqlite => sqlite.MigrationsAssembly(DependencyInjection.SqliteMigrationsAssembly));
        }
        else
        {
            builder.UseSqlServer(
                connection ?? "Server=(localdb)\\mssqllocaldb;Database=DentalSurgery-Design;Trusted_Connection=True",
                sql => sql.MigrationsAssembly(DependencyInjection.SqlServerMigrationsAssembly));
        }

        // Design time has no request and therefore no tenant; scaffolding the
        // model must see every entity.
        return new DentalDbContext(builder.Options, PlatformTenantContext.Instance);
    }
}
