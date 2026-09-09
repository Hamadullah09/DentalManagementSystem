namespace DentalSurgery.Domain.Common;

/// <summary>
/// An entity that belongs to one tenant and must never be visible to another.
/// <para>
/// Everything carrying this is filtered by tenant on every query and stamped on
/// every insert. Nothing in the application layer opts out; the filter is
/// applied in the model, not by callers remembering to add a <c>Where</c>.
/// </para>
/// </summary>
public interface ITenantScoped
{
    Guid TenantId { get; set; }
}

/// <summary>
/// An entity deliberately shared by every tenant: clinical reference data that
/// is the same for all practices — tooth anatomy, the procedure code catalogue,
/// the allergen, condition and drug catalogues.
/// <para>
/// This is an explicit opt-out and the model guard rejects any entity that is
/// neither this nor <see cref="ITenantScoped"/>. Silence is not permitted,
/// because an entity nobody classified is an entity nobody filtered.
/// </para>
/// </summary>
public interface IGlobalEntity;

/// <summary>
/// A tenant: one dental business with its own records, staff and logins.
/// <para>
/// Kept distinct from <c>Practice</c> on purpose. The tenant is the account
/// boundary the platform enforces; the practice is the clinical business record
/// that lives inside it. Separating them keeps the filter uniform — every
/// scoped entity, practice included, is filtered on the same
/// <c>TenantId</c> column with the same predicate — and leaves room for a
/// tenant to hold more than one practice later without remodelling.
/// </para>
/// </summary>
public class Tenant : BaseEntity, IGlobalEntity
{
    /// <summary>The business name, as it appears to the platform operator.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL-safe identifier used to resolve the tenant from a host name, for
    /// example "meridian" in meridian.example.com. Unique across the platform.
    /// </summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// A suspended tenant cannot be signed into. Sign-in is refused rather than
    /// the data being hidden, so a billing suspension does not present to the
    /// practice as their records having vanished.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime? SuspendedAtUtc { get; set; }
    public string? SuspendedReason { get; set; }
}

/// <summary>
/// Base for entities owned by a tenant. Deriving from this, rather than from
/// <see cref="BaseEntity"/>, is what puts an entity inside the tenant boundary.
/// </summary>
public abstract class TenantEntity : BaseEntity, ITenantScoped
{
    public Guid TenantId { get; set; }
}

/// <summary>
/// Reference-shaped data that nevertheless belongs to one practice: message
/// templates, consent forms, post-operative instructions. Same shape as
/// <see cref="LookupEntity"/>, inside the tenant boundary.
/// </summary>
public abstract class TenantLookupEntity : LookupEntity, ITenantScoped
{
    public Guid TenantId { get; set; }
}
