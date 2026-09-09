using DentalSurgery.Application.Abstractions;
using DentalSurgery.Infrastructure.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace DentalSurgery.Tests;

/// <summary>
/// A single fixed tenant for suites that are testing something other than
/// tenancy.
/// <para>
/// They still run inside a real tenant rather than a platform scope, because
/// roles, permission grants and clinical records are all tenant-owned now: a
/// fixture that seeded outside a tenant would be testing a shape the running
/// system never has. Cross-tenant behaviour itself is covered by
/// <see cref="TenantIsolationTests"/>.
/// </para>
/// </summary>
public static class TestTenancy
{
    public static readonly Guid TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>A context already bound to the fixed tenant.</summary>
    public static TenantContext Bound()
    {
        var context = new TenantContext(NullLogger<TenantContext>.Instance);
        context.Bind(TenantId);
        return context;
    }

    /// <summary>
    /// Registers the tenancy services a <c>PermissionCatalogue</c> resolves when
    /// it opens its own scope to rebuild the permission map.
    /// </summary>
    public static IServiceCollection AddTestTenancy(this IServiceCollection services)
    {
        services.AddScoped(_ => Bound());
        services.AddScoped<ITenantContext>(p => p.GetRequiredService<TenantContext>());
        services.AddScoped<ITenantScopeFactory>(p => p.GetRequiredService<TenantContext>());
        return services;
    }
}
