using Microsoft.AspNetCore.Identity;

namespace DentalSurgery.Infrastructure.Identity;

/// <summary>A login account. Linked to a <c>Staff</c> record for clinical attribution.</summary>
public class ApplicationUser : IdentityUser
{
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

/// <summary>A role, extended with a description shown in the admin screens.</summary>
public class ApplicationRole : IdentityRole
{
    public ApplicationRole() { }

    public ApplicationRole(string roleName) : base(roleName) { }

    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public int SortOrder { get; set; }
}
