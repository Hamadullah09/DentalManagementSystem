using DentalSurgery.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DentalSurgery.Infrastructure.Persistence;

/// <summary>
/// Used by <c>dotnet ef</c> at design time. The runtime application builds its
/// own context from configuration; this exists so migrations can be scaffolded,
/// and applied, without starting the web host.
/// <para>
/// The provider is chosen by the <c>DENTAL_MIGRATION_PROVIDER</c> environment
/// variable, because the two providers keep their migrations in separate
/// assemblies and each must be scaffolded against its own dialect. Scaffolding
/// needs no database: EF only needs a configured provider to generate the SQL.
/// </para>
/// <para>
/// The connection comes from a <c>cs=</c> argument, or failing that from
/// <c>ConnectionStrings__DefaultConnection</c> in the environment, before the
/// local development default. Reading the environment matters because
/// <c>dotnet ef</c> prefers this factory over the application's own host: a
/// release step running <c>database update</c> against a configured server was
/// silently sent to the local default instead, and failed with "LocalDB is not
/// supported on this platform" rather than anything naming the real cause. The
/// environment is used rather than an argument so the connection string, which
/// carries the database password, stays out of the process list and the logs.
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

        if (string.IsNullOrWhiteSpace(connection))
        {
            // The same key the application reads, so a deployment configures one
            // thing rather than two that must be kept in step.
            var configured = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

            if (!string.IsNullOrWhiteSpace(configured)) connection = configured;
        }

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
