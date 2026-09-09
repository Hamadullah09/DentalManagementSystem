using DentalSurgery.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace DentalSurgery.Infrastructure.Identity;

/// <summary>A login account. Linked to a <c>Staff</c> record for clinical attribution.</summary>
public class ApplicationUser : IdentityUser, ITenantScoped
{
    /// <summary>
    /// The practice this login belongs to. Every account is owned by exactly one
    /// tenant, and it is this value - carried into the sign-in cookie as a claim
    /// - that decides what the whole session can see.
    /// </summary>
    public Guid TenantId { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    /// <summary>The staff record this login acts as, when there is one.</summary>
    public Guid? StaffId { get; set; }

    public string? JobTitle { get; set; }
    public Guid? DefaultLocationId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool MustChangePassword { get; set; }
    public DateTime? LastLoginUtc { get; set; }
    public string? LastLoginIp { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? AvatarPath { get; set; }
    public string ThemePreference { get; set; } = "auto";

    public string DisplayName =>
        string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName)
            ? UserName ?? Email ?? "User"
            : $"{FirstName} {LastName}".Trim();

    public string Initials
    {
        get
        {
            var first = string.IsNullOrWhiteSpace(FirstName) ? null : FirstName![0].ToString();
            var last = string.IsNullOrWhiteSpace(LastName) ? null : LastName![0].ToString();
            var initials = $"{first}{last}".ToUpperInvariant();
            return string.IsNullOrEmpty(initials)
                ? (UserName ?? "?")[..1].ToUpperInvariant()
                : initials;
        }
    }
}

/// <summary>
/// A role, extended with a description shown in the admin screens.
/// <para>
/// Tenant-scoped, because a role carries its permission grants as claims and
/// the administration screens let a practice edit them. Were roles shared, one
/// practice widening what its receptionists may do would widen it for every
/// practice on the platform — a privilege escalation reachable from an ordinary
/// admin screen. Each tenant therefore owns its own copy of the standard roles.
/// </para>
/// </summary>
public class ApplicationRole : IdentityRole, ITenantScoped
{
    public ApplicationRole() { }

    public ApplicationRole(string roleName) : base(roleName) { }

    public Guid TenantId { get; set; }

    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public int SortOrder { get; set; }
}
