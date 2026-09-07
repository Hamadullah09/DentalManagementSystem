namespace DentalSurgery.Infrastructure.Persistence.Seed;

/// <summary>
/// Controls what a fresh database is populated with.
/// <para>
/// Every default here is the safe one. Demonstration data is off, the
/// demonstration staff logins are off, and there is no built-in administrator
/// password: outside Development the operator must supply one or start-up
/// fails. A deployment that forgets to configure this gets an empty, locked
/// system rather than a set of accounts on a password published in the README.
/// </para>
/// </summary>
public class SeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>
    /// Creates the demonstration practice, staff, patients and history.
    /// Ignored outside Development unless <see cref="AllowDemoDataOutsideDevelopment"/>
    /// is also set, because demo staff are created with a shared known password.
    /// </summary>
    public bool DemoData { get; set; }

    /// <summary>
    /// Permits demonstration data in a non-Development environment. Intended for
    /// a staging or training instance that deliberately holds fabricated
    /// records. Never set this where real patient data will live: it creates
    /// logins whose password is shared and publicly documented.
    /// </summary>
    public bool AllowDemoDataOutsideDevelopment { get; set; }

    /// <summary>The first administrator's sign-in address.</summary>
    public string AdminEmail { get; set; } = "admin@dentalsurgery.local";

    /// <summary>
    /// The first administrator's password. Required outside Development; supply
    /// it through an environment variable or secret store, never appsettings.
    /// The account is created with <c>MustChangePassword</c>, so this is a
    /// bootstrap value rather than a standing credential.
    /// </summary>
    public string? AdminPassword { get; set; }

    /// <summary>
    /// Applies pending migrations during start-up. Correct for a single-instance
    /// install; wrong for a multi-instance deployment, where concurrent hosts
    /// race to migrate. Leave off there and migrate as a release step, which is
    /// what <c>MigrateOnStartup=false</c> plus the schema check enforces.
    /// </summary>
    public bool MigrateOnStartup { get; set; }

    /// <summary>
    /// Refuses to start when the schema is behind the code. The alternative is
    /// serving requests against a database missing columns the queries expect,
    /// which surfaces as scattered runtime failures instead of one clear one.
    /// </summary>
    public bool VerifySchemaOnStartup { get; set; } = true;
}
