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

    /// <summary>
    /// The first tenant's display name. Every install has at least one tenant,
    /// because all practice data belongs to one.
    /// </summary>
    public string TenantName { get; set; } = "Meridian Dental Surgery";

    /// <summary>
    /// The first tenant's slug, used to resolve it from a host name. Must be
    /// unique across the platform.
    /// </summary>
    public string TenantSlug { get; set; } = "meridian";

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
    /// The logins to provision, and the only place account passwords come from.
    /// <para>
    /// Supplied through user secrets in development
    /// (<c>dotnet user-secrets set "Seed:Accounts:0:Password" "..."</c>) or the
    /// environment in a deployment
    /// (<c>Seed__Accounts__0__Password</c>). Never in appsettings.json, and
    /// never in source: a password written into a file inside the repository is
    /// a password published the moment anyone pushes.
    /// </para>
    /// <para>
    /// Empty by default. A build with no accounts configured creates the
    /// bootstrap administrator only, and the demonstration staff get no logins
    /// at all rather than a shared one everybody knows.
    /// </para>
    /// </summary>
    public List<SeedAccount> Accounts { get; set; } = [];

    /// <summary>
    /// Refuses to start when the schema is behind the code. The alternative is
    /// serving requests against a database missing columns the queries expect,
    /// which surfaces as scattered runtime failures instead of one clear one.
    /// </summary>
    public bool VerifySchemaOnStartup { get; set; } = true;
}


/// <summary>
/// One login to provision. Passwords are supplied at deployment time, so this
/// carries no defaults.
/// </summary>
public class SeedAccount
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    /// <summary>A role name from <c>Roles</c>, for example "Dentist".</summary>
    public string Role { get; set; } = string.Empty;

    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    /// <summary>
    /// Links the login to a seeded staff record, so the account signs in as a
    /// clinician who already has a diary, patients and history rather than as a
    /// stranger with the right role and nothing to look at. Ignored when the
    /// staff record does not exist.
    /// </summary>
    public string? StaffNumber { get; set; }

    /// <summary>
    /// Forces a password change at first sign-in. On by default: a password
    /// someone else chose and typed into a configuration file is a bootstrap
    /// value, not a standing credential.
    /// </summary>
    public bool MustChangePassword { get; set; } = true;
}
