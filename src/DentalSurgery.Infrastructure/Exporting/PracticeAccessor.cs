using DentalSurgery.Application.Abstractions;
using DentalSurgery.Domain.Entities;
using DentalSurgery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DentalSurgery.Infrastructure.Exporting;

/// <summary>
/// The practice a document is being produced for, cached because it changes at
/// most a few times a year and every page of every PDF asks for it.
/// <para>
/// Scoped, and the cache is keyed by tenant. It was previously a singleton that
/// opened a scope of its own to read the practice — and that scope has no
/// tenant, so once records became tenant-scoped the query matched nothing and
/// the accessor cached <c>null</c> permanently. Every document then fell back to
/// a generic letterhead with no practice name, address or logo, which is the
/// sort of failure that looks like a design decision rather than a bug.
/// </para>
/// </summary>
public class PracticeAccessor(
    IDbContextFactory<DentalDbContext> dbFactory,
    ITenantContext tenant,
    IMemoryCache cache) : IPracticeAccessor
{
    private static string KeyFor(Guid tenantId) => $"dental:practice:{tenantId}";

    public Practice? Current
    {
        get
        {
            // No tenant means no practice to speak for. Returning null lets the
            // letterhead fall back rather than showing another tenant's details.
            if (tenant.TenantId is not { } tenantId) return null;

            if (cache.TryGetValue(KeyFor(tenantId), out Practice? cached)) return cached;

            Practice? practice;

            try
            {
                using var db = dbFactory.CreateDbContext();
                practice = db.Practices.AsNoTracking().FirstOrDefault();
            }
            catch (Exception)
            {
                // A document must still render if the lookup fails; the
                // letterhead simply falls back to generic text. Not cached, so
                // a transient database fault does not persist for the life of
                // the process.
                return null;
            }

            cache.Set(KeyFor(tenantId), practice, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
            });

            return practice;
        }
    }

    public void Invalidate()
    {
        if (tenant.TenantId is { } tenantId) cache.Remove(KeyFor(tenantId));
    }
}
