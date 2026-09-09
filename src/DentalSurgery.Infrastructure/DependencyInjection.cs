using DentalSurgery.Application.Abstractions;
using DentalSurgery.Application.Clinical;
using DentalSurgery.Application.Exporting;
using DentalSurgery.Application.Scheduling;
using DentalSurgery.Infrastructure.Claims;
using DentalSurgery.Infrastructure.Configuration;
using DentalSurgery.Infrastructure.Exporting;
using DentalSurgery.Infrastructure.Identity;
using DentalSurgery.Infrastructure.Notifications;
using DentalSurgery.Infrastructure.Persistence;
using DentalSurgery.Infrastructure.Persistence.Interceptors;
using DentalSurgery.Infrastructure.Persistence.Seed;
using DentalSurgery.Infrastructure.Services;
using DentalSurgery.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DentalSurgery.Infrastructure;

/// <summary>Which relational store the application is running against.</summary>
public enum DatabaseProvider
{
    /// <summary>Production. Native decimal, native rowversion, real concurrency.</summary>
    SqlServer = 0,

    /// <summary>Local development and tests. Single writer, no decimal type.</summary>
    Sqlite = 1
}

public static class DependencyInjection
{
    /// <summary>
    /// Migrations are provider-specific: the generated SQL differs by dialect.
    /// EF discovers every migration in a migrations assembly regardless of
    /// namespace, so the two sets cannot share one — hence a project each,
    /// selected alongside the provider.
    /// </summary>
    public const string SqlServerMigrationsAssembly = "DentalSurgery.Migrations.SqlServer";
    public const string SqliteMigrationsAssembly = "DentalSurgery.Migrations.Sqlite";

    /// <summary>
    /// Reads <c>Database:Provider</c>. Defaults to SQL Server, so an unset value
    /// gives the production store rather than silently writing a practice's
    /// records to a local file.
    /// </summary>
    public static DatabaseProvider ResolveProvider(IConfiguration configuration)
    {
        var configured = configuration["Database:Provider"];

        if (string.IsNullOrWhiteSpace(configured)) return DatabaseProvider.SqlServer;

        if (Enum.TryParse<DatabaseProvider>(configured, ignoreCase: true, out var provider))
            return provider;

        throw new InvalidOperationException(
            $"Database:Provider '{configured}' is not recognised. Use 'SqlServer' or 'Sqlite'.");
    }

    /// <summary>
    /// Registers persistence, domain services and the supporting platform
    /// services. The host is responsible for registering <see cref="ICurrentUser"/>;
    /// a system fallback is used when it does not.
    /// </summary>
    public static IServiceCollection AddDentalInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var databaseProvider = ResolveProvider(configuration);
        var rawConnectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(rawConnectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is not set. Supply it through an environment " +
                "variable or secret store (ConnectionStrings__DefaultConnection). It is deliberately " +
                "empty in appsettings.json so that a deployment cannot fall back to a local file " +
                "database without noticing.");
        }

        var connectionString = databaseProvider == DatabaseProvider.Sqlite
            ? ResolveSqliteConnectionString(rawConnectionString)
            : rawConnectionString;

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.TryAddCurrentUserFallback();

        // ---- tenancy ---------------------------------------------------------
        // One instance per scope, serving both interfaces: reading the current
        // tenant and entering a different one are the same object, so a scope
        // cannot be handed a reader that disagrees with its writer.
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(p => p.GetRequiredService<TenantContext>());
        services.AddScoped<ITenantScopeFactory>(p => p.GetRequiredService<TenantContext>());
        services.AddScoped<TenantProvisioningService>();

        services.AddScoped<AuditingInterceptor>();
        services.AddScoped<TenantGuardInterceptor>();

        // A scoped factory, because Blazor Server components can start
        // overlapping queries within one circuit and a DbContext is not
        // re-entrant. UI components create a short-lived context from the
        // factory; application services use the scoped instance below.
        services.AddDbContextFactory<DentalDbContext>((provider, options) =>
        {
            if (databaseProvider == DatabaseProvider.Sqlite)
            {
                options.UseSqlite(connectionString, sqlite =>
                {
                    sqlite.MigrationsAssembly(SqliteMigrationsAssembly);
                    sqlite.CommandTimeout(60);
                });
            }
            else
            {
                options.UseSqlServer(connectionString, sql =>
                {
                    sql.MigrationsAssembly(SqlServerMigrationsAssembly);
                    sql.CommandTimeout(60);

                    // A managed database fails over and throttles. Without a
                    // retry strategy those appear to the user as random errors
                    // in the middle of clinical work.
                    sql.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null);
                });
            }

            // The tenant guard runs first: a write that crosses a boundary must
            // be refused before the audit interceptor records it as having
            // happened.
            options.AddInterceptors(
                provider.GetRequiredService<TenantGuardInterceptor>(),
                provider.GetRequiredService<AuditingInterceptor>());

            // Query filters on soft-deleted entities interact with required
            // navigations; the warning is expected and would otherwise be noise.
            options.ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId
                    .PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
        }, ServiceLifetime.Scoped);

        services.AddScoped(provider =>
            provider.GetRequiredService<IDbContextFactory<DentalDbContext>>().CreateDbContext());

        services.AddScoped<INumberSequenceService, NumberSequenceService>();

        // ---- seeding ---------------------------------------------------------
        // Bound rather than defaulted, so an absent section yields the safe
        // values on SeedOptions rather than the demonstration ones.
        services.AddOptions<SeedOptions>()
            .Bind(configuration.GetSection(SeedOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<SeedOptions>, SeedOptionsValidator>();

        // ---- messaging -----------------------------------------------------
        services.AddOptions<NotificationOptions>()
            .Bind(configuration.GetSection(NotificationOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<NotificationOptions>, NotificationOptionsValidator>();
        services.AddHttpClient("sms");
        services.AddScoped<SmtpEmailSender>();
        services.AddScoped<HttpSmsSender>();
        services.AddScoped<INotificationSender, CompositeNotificationSender>();
        services.AddHostedService<ReminderDispatcher>();

        // ---- claim submission ------------------------------------------------
        services.AddOptions<ClaimSubmissionOptions>()
            .Bind(configuration.GetSection(ClaimSubmissionOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<ClaimSubmissionOptions>, ClaimSubmissionOptionsValidator>();
        services.AddHttpClient("claims");
        services.AddScoped<X12ClaimGenerator>();
        services.AddScoped<FileDropClaimGateway>();
        services.AddScoped<HttpClaimGateway>();
        services.AddScoped<ManualClaimGateway>();
        services.AddScoped<ClaimGatewaySelector>();
        services.AddScoped<ClaimSubmissionService>();

        // ---- exporting --------------------------------------------------------
        // The practice letterhead is cached across requests.
        services.AddSingleton<IPracticeAccessor, PracticeAccessor>();
        services.AddSingleton<IReportExporter, CsvReportExporter>();
        services.AddSingleton<IReportExporter, JsonReportExporter>();
        services.AddSingleton<IReportExporter, ExcelReportExporter>();
        services.AddSingleton<IReportExporter, PdfReportExporter>();
        services.AddScoped<ReportCatalogue>();
        services.AddScoped<ExportService>();

        // ---- documents and pricing ----------------------------------------------
        services.AddScoped<DocumentService>();
        services.AddScoped<FeeScheduleImporter>();

        services.AddScoped<IFileStorage>(provider =>
        {
            var root = configuration["Storage:DocumentRoot"]
                       ?? Path.Combine(AppContext.BaseDirectory, "App_Data", "documents");
            Directory.CreateDirectory(root);
            return new LocalFileStorage(root, provider.GetRequiredService<ILogger<LocalFileStorage>>());
        });

        // Stateless calculators, safe to share.
        services.AddSingleton<MedicalRiskAssessor>();
        services.AddSingleton<PeriodontalAnalyser>();
        services.AddSingleton<DentalChartBuilder>();
        services.AddSingleton<AvailabilityCalculator>();

        // Authorisation for the service layer. The catalogue holds the
        // role-to-permission map; the guard is what a service calls to refuse
        // work the operator is not entitled to.
        services.AddMemoryCache();
        services.AddSingleton<PermissionCatalogue>();
        services.TryAddPermissionGuardFallback();

        services.AddScoped<PatientService>();
        services.AddScoped<AppointmentService>();
        services.AddScoped<ClinicalService>();
        services.AddScoped<TreatmentPlanService>();
        services.AddScoped<BillingService>();
        services.AddScoped<InventoryService>();
        services.AddScoped<ProcedureService>();
        services.AddScoped<ReportingService>();
        services.AddScoped<DatabaseInitialiser>();

        return services;
    }

    /// <summary>
    /// The practice password, lockout and sign-in policy. The web host applies
    /// this when it registers Identity, since the sign-in manager and cookie
    /// handlers live in the ASP.NET Core framework rather than here.
    /// </summary>
    public static void ConfigureIdentityOptions(IdentityOptions options)
    {
        options.SignIn.RequireConfirmedAccount = false;

        options.Password.RequiredLength = 10;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;

        options.User.RequireUniqueEmail = true;
    }

    /// <summary>
    /// Anchors a relative data source to the application directory and creates
    /// the folder. SQLite resolves relative paths against the current working
    /// directory, which differs between "dotnet run" and a published build, and
    /// it will not create a missing folder for itself.
    /// </summary>
    private static string ResolveSqliteConnectionString(string connectionString)
    {
        Microsoft.Data.Sqlite.SqliteConnectionStringBuilder builder;
        try
        {
            builder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connectionString);
        }
        catch (ArgumentException)
        {
            // A malformed string is reported by the provider with a better message.
            return connectionString;
        }

        var dataSource = builder.DataSource;
        if (string.IsNullOrWhiteSpace(dataSource)) return connectionString;
        if (dataSource.StartsWith(":memory:", StringComparison.OrdinalIgnoreCase)) return connectionString;

        if (!Path.IsPathRooted(dataSource))
        {
            dataSource = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, dataSource));
            builder.DataSource = dataSource;
        }

        var directory = Path.GetDirectoryName(dataSource);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

        return builder.ToString();
    }

    private static void TryAddPermissionGuardFallback(this IServiceCollection services)
    {
        // The web host registers the real guard against the signed-in user.
        // Anything else hosting this assembly — the seeder, a background
        // dispatcher, a test fixture — has no operator to check, so it gets the
        // system guard rather than silently failing every permission.
        if (services.Any(d => d.ServiceType == typeof(IPermissionGuard))) return;
        services.AddScoped<IPermissionGuard, SystemPermissionGuard>();
    }

    private static void TryAddCurrentUserFallback(this IServiceCollection services)
    {
        if (services.Any(d => d.ServiceType == typeof(ICurrentUser))) return;
        services.AddScoped<ICurrentUser, SystemCurrentUser>();
    }
}
