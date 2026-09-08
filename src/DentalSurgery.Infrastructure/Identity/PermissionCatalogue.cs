using DentalSurgery.Application.Abstractions;
using DentalSurgery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DentalSurgery.Infrastructure.Identity;

/// <summary>
/// Resolves role names to the permissions those roles currently grant.
/// <para>
/// The grant lives in the database as a role claim, which is what lets an
/// administrator change it without a deployment. Reading it on every
/// authorisation check would put a query in front of every page and every API
/// call, so the whole map — it is a few hundred short strings — is held in
/// memory and rebuilt when it changes.
/// </para>
/// <para>
/// Deliberately not stamped onto the sign-in cookie. A permission removed from
/// a role has to take effect on the user's next request, not whenever their
/// cookie happens to expire.
/// </para>
/// </summary>
public class PermissionCatalogue(
    IServiceScopeFactory scopeFactory,
    IMemoryCache cache,
    ILogger<PermissionCatalogue> logger)
{
    /// <summary>The claim type a role's permission grants are stored under.</summary>
    public const string ClaimType = "dental:permission";

    private const string CacheKey = "dental:role-permissions";

    private static readonly SemaphoreSlim Gate = new(1, 1);

    /// <summary>The permissions granted by each role, keyed by role name.</summary>
    public async Task<IReadOnlyDictionary<string, IReadOnlySet<string>>> MapAsync(
        CancellationToken ct = default)
    {
        if (cache.TryGetValue(CacheKey, out IReadOnlyDictionary<string, IReadOnlySet<string>>? cached)
            && cached is not null)
        {
            return cached;
        }

        await Gate.WaitAsync(ct);
        try
        {
            // A second caller may have populated it while this one queued.
            if (cache.TryGetValue(CacheKey, out cached) && cached is not null) return cached;

            var map = await BuildAsync(ct);

            // No expiry: the only thing that changes this map is a write through
            // Invalidate below, and an entry that silently expired would put a
            // query back in front of every request for no benefit.
            cache.Set(CacheKey, map, new MemoryCacheEntryOptions { Priority = CacheItemPriority.NeverRemove });
            return map;
        }
        finally
        {
            Gate.Release();
        }
    }

    private async Task<IReadOnlyDictionary<string, IReadOnlySet<string>>> BuildAsync(CancellationToken ct)
    {
        // The catalogue is a singleton so the map is shared and built once; the
        // database context it reads from is scoped. Opening a scope here is what
        // reconciles the two, rather than lengthening the context's lifetime to
        // match the cache's.
        using var scope = scopeFactory.CreateScope();

        var dbFactory = scope.ServiceProvider
            .GetRequiredService<IDbContextFactory<DentalDbContext>>();

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var rows = await (
            from claim in db.RoleClaims.AsNoTracking()
            join role in db.Roles.AsNoTracking() on claim.RoleId equals role.Id
            where claim.ClaimType == ClaimType && claim.ClaimValue != null
            select new { RoleName = role.Name!, Permission = claim.ClaimValue! })
            .ToListAsync(ct);

        var map = rows
            .GroupBy(r => r.RoleName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlySet<string>)g.Select(r => r.Permission)
                    .ToHashSet(StringComparer.Ordinal),
                StringComparer.OrdinalIgnoreCase);

        logger.LogInformation(
            "Loaded the permission map: {RoleCount} role(s), {GrantCount} grant(s).",
            map.Count, rows.Count);

        return map;
    }

    /// <summary>The union of everything a set of roles grants.</summary>
    public async Task<IReadOnlySet<string>> ForRolesAsync(
        IEnumerable<string> roles, CancellationToken ct = default)
    {
        var map = await MapAsync(ct);
        var held = new HashSet<string>(StringComparer.Ordinal);

        foreach (var role in roles)
        {
            if (map.TryGetValue(role, out var granted)) held.UnionWith(granted);
        }

        return held;
    }

    /// <summary>Drops the cached map so the next check reads the database again.</summary>
    public void Invalidate()
    {
        cache.Remove(CacheKey);
        logger.LogInformation("The permission map was invalidated and will be rebuilt on the next check.");
    }
}

/// <summary>
/// The service-layer authorisation check. See <see cref="IPermissionGuard"/>
/// for why this exists alongside the route attributes rather than instead of them.
/// </summary>
public class PermissionGuard(
    ICurrentUser currentUser,
    PermissionCatalogue catalogue,
    ILogger<PermissionGuard> logger) : IPermissionGuard
{
    private IReadOnlySet<string>? _cached;

    public async Task<IReadOnlySet<string>> CurrentAsync(CancellationToken ct = default)
    {
        // Scoped to the request, so resolving once is enough and a page that
        // asks about a dozen permissions still costs one lookup.
        if (_cached is not null) return _cached;

        if (!currentUser.IsAuthenticated)
        {
            _cached = new HashSet<string>(StringComparer.Ordinal);
            return _cached;
        }

        _cached = await catalogue.ForRolesAsync(currentUser.Roles, ct);
        return _cached;
    }

    public async Task<bool> HasAsync(string permission, CancellationToken ct = default) =>
        (await CurrentAsync(ct)).Contains(permission);

    public async Task<bool> HasAllAsync(IEnumerable<string> permissions, CancellationToken ct = default)
    {
        var held = await CurrentAsync(ct);
        return permissions.All(held.Contains);
    }

    public async Task<bool> HasAnyAsync(IEnumerable<string> permissions, CancellationToken ct = default)
    {
        var held = await CurrentAsync(ct);
        return permissions.Any(held.Contains);
    }

    public async Task DemandAsync(string permission, CancellationToken ct = default)
    {
        if (await HasAsync(permission, ct)) return;

        // Logged as a security event: a refusal here means something reached the
        // service that the interface would not have offered.
        logger.LogWarning(
            "Refused {Permission} for {User} from {Ip}. Roles held: {Roles}.",
            permission,
            currentUser.UserName ?? "(anonymous)",
            currentUser.IpAddress ?? "(unknown)",
            currentUser.Roles.Count == 0 ? "(none)" : string.Join(", ", currentUser.Roles));

        throw new ForbiddenException(permission);
    }
}

/// <summary>
/// A guard for code running outside a request — seeding, migrations and
/// background dispatch. It grants everything, because there is no operator to
/// refuse and the work is already trusted by virtue of being in-process.
/// </summary>
public class SystemPermissionGuard : IPermissionGuard
{
    public Task<bool> HasAsync(string permission, CancellationToken ct = default) => Task.FromResult(true);

    public Task<bool> HasAllAsync(IEnumerable<string> permissions, CancellationToken ct = default) =>
        Task.FromResult(true);

    public Task<bool> HasAnyAsync(IEnumerable<string> permissions, CancellationToken ct = default) =>
        Task.FromResult(true);

    public Task DemandAsync(string permission, CancellationToken ct = default) => Task.CompletedTask;

    public Task<IReadOnlySet<string>> CurrentAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlySet<string>>(Permissions.All.ToHashSet(StringComparer.Ordinal));
}
