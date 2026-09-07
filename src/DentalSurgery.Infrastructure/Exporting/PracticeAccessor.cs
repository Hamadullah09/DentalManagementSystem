using DentalSurgery.Domain.Entities;
using DentalSurgery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DentalSurgery.Infrastructure.Exporting;

/// <summary>
/// Caches the practice record for letterheads. It changes at most a few times a
/// year, so a singleton cache avoids a query on every generated document.
/// </summary>
public class PracticeAccessor(IServiceScopeFactory scopeFactory) : IPracticeAccessor
{
    private readonly object _gate = new();
    private Practice? _cached;
    private bool _loaded;

    public Practice? Current
    {
        get
        {
            lock (_gate)
            {
                if (_loaded) return _cached;

                try
                {
                    // The accessor is a singleton but the context factory is registered
                    // per request, so it has to be resolved inside a scope of its own.
                    using var scope = scopeFactory.CreateScope();
                    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<DentalDbContext>>();
                    using var db = dbFactory.CreateDbContext();
                    _cached = db.Practices.AsNoTracking().FirstOrDefault();
                }
                catch (Exception)
                {
                    // A document must still render if the lookup fails; the
                    // letterhead simply falls back to generic text.
                    _cached = null;
                }

                _loaded = true;
                return _cached;
            }
        }
    }

    public void Invalidate()
    {
        lock (_gate)
        {
            _cached = null;
            _loaded = false;
        }
    }
}
