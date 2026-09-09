using DentalSurgery.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace DentalSurgery.Infrastructure.Tenancy;

/// <summary>
/// The ambient tenant for one scope — one HTTP request, one Blazor circuit
/// operation, or one iteration of a background sweep.
/// <para>
/// Scoped rather than singleton, and mutable only through
/// <see cref="ITenantScopeFactory"/>, so nothing reachable from a request can
/// widen its own visibility.
/// </para>
/// </summary>
public class TenantContext : ITenantContext, ITenantScopeFactory
{
    private readonly ILogger<TenantContext> _logger;

    private Guid? _tenantId;
    private int _platformDepth;

    public TenantContext(ILogger<TenantContext> logger) => _logger = logger;

    public Guid? TenantId => _tenantId;

    public bool IsPlatformScope => _platformDepth > 0;

    public Guid RequireTenantId()
    {
        if (_tenantId is { } id && id != Guid.Empty) return id;

        throw new InvalidOperationException(
            "No tenant is in scope, so this row cannot be written. A write must happen either " +
            "inside a request whose user belongs to a tenant, or inside an explicit " +
            "ITenantScopeFactory.EnterTenant scope.");
    }

    /// <summary>
    /// Binds the scope to a tenant. Called once by the host's resolution
    /// middleware immediately after authentication; calling it again with a different tenant is a bug
    /// serious enough to fail rather than silently re-point the scope.
    /// </summary>
    public void Bind(Guid tenantId)
    {
        if (_tenantId is { } existing && existing != tenantId)
        {
            throw new InvalidOperationException(
                $"The tenant scope is already bound to {existing} and cannot be re-bound to " +
                $"{tenantId}. A scope belongs to one tenant for its whole life.");
        }

        _tenantId = tenantId;
    }

    public IDisposable EnterTenant(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("A tenant scope needs a tenant.", nameof(tenantId));

        var previous = _tenantId;
        _tenantId = tenantId;
        return new Scope(() => _tenantId = previous);
    }

    public IDisposable EnterPlatformScope(string reason)
    {
        // Logged at every entry. This is the only way to see across tenants, so
        // an operator investigating a leak needs a record of each time it
        // happened and why.
        _logger.LogInformation("Entering platform scope: {Reason}", reason);

        _platformDepth++;
        return new Scope(() =>
        {
            _platformDepth--;
            _logger.LogDebug("Left platform scope: {Reason}", reason);
        });
    }

    private sealed class Scope(Action onDispose) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            onDispose();
        }
    }
}

/// <summary>
/// A context permanently in platform scope, for code that legitimately runs
/// outside any tenant: the design-time factory that scaffolds migrations, and
/// tests that assert on the whole model.
/// <para>
/// Never registered in the application container. The runtime registration is
/// <see cref="TenantContext"/>, which starts with no tenant and no platform
/// scope, so a request that fails to resolve a tenant sees nothing.
/// </para>
/// </summary>
public sealed class PlatformTenantContext : ITenantContext
{
    public static readonly PlatformTenantContext Instance = new();

    public Guid? TenantId => null;

    public bool IsPlatformScope => true;

    public Guid RequireTenantId() => throw new InvalidOperationException(
        "The platform context has no tenant to write against. Enter a tenant scope first.");
}
