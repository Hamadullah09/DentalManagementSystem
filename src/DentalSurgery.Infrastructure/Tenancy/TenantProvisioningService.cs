using DentalSurgery.Application.Abstractions;
using DentalSurgery.Domain.Common;
using DentalSurgery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace DentalSurgery.Infrastructure.Tenancy;

/// <summary>
/// Creates and looks up tenants. The only code permitted to write to the tenant
/// registry, and the only place a tenant comes into existence.
/// </summary>
public partial class TenantProvisioningService(
    DentalDbContext db,
    ITenantScopeFactory scopeFactory,
    ILogger<TenantProvisioningService> logger)
{
    /// <summary>
    /// Creates a tenant, or returns the existing one with the same slug.
    /// Idempotent, so the seeder can call it on every start.
    /// </summary>
    public async Task<Tenant> EnsureTenantAsync(string name, string slug, CancellationToken ct = default)
    {
        var normalised = NormaliseSlug(slug);

        // Provisioning necessarily happens outside any tenant: there is not yet
        // a tenant to be inside.
        using var _ = scopeFactory.EnterPlatformScope($"provisioning tenant '{normalised}'");

        var existing = await db.Tenants.FirstOrDefaultAsync(t => t.Slug == normalised, ct);
        if (existing is not null) return existing;

        var tenant = new Tenant
        {
            Name = name,
            Slug = normalised,
            IsActive = true
        };

        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Provisioned tenant {Name} ({Slug}) as {Id}.", name, normalised, tenant.Id);
        return tenant;
    }

    /// <summary>Resolves a tenant by its host slug, for sign-in.</summary>
    public async Task<Tenant?> FindBySlugAsync(string slug, CancellationToken ct = default)
    {
        using var _ = scopeFactory.EnterPlatformScope("resolving tenant by slug");
        var normalised = NormaliseSlug(slug);
        return await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Slug == normalised, ct);
    }

    public async Task<Tenant?> FindByIdAsync(Guid id, CancellationToken ct = default)
    {
        using var _ = scopeFactory.EnterPlatformScope("resolving tenant by id");
        return await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    /// <summary>Every tenant, for a background sweep that must visit each in turn.</summary>
    public async Task<IReadOnlyList<Tenant>> ActiveTenantsAsync(CancellationToken ct = default)
    {
        using var _ = scopeFactory.EnterPlatformScope("enumerating active tenants");
        return await db.Tenants.AsNoTracking()
            .Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Lower-cases and strips anything that would not survive a host name, so a
    /// slug can be matched against a subdomain without further escaping.
    /// </summary>
    public static string NormaliseSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("A tenant needs a slug.", nameof(slug));

        var cleaned = SlugPattern().Replace(slug.Trim().ToLowerInvariant(), "-").Trim('-');

        if (cleaned.Length == 0)
            throw new ArgumentException($"'{slug}' contains no characters usable in a slug.", nameof(slug));

        return cleaned;
    }

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex SlugPattern();
}
