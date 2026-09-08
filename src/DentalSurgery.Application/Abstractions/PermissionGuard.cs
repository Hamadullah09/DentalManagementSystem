namespace DentalSurgery.Application.Abstractions;

/// <summary>
/// Thrown when the signed-in operator is not permitted to carry out an
/// operation. The web layer turns this into a 403 for an API caller and an
/// access-denied screen for a browser; it must never surface as a stack trace.
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string permission)
        : base($"This account does not hold the '{permission}' permission.")
        => Permission = permission;

    public ForbiddenException(string permission, string message) : base(message)
        => Permission = permission;

    /// <summary>The permission that was demanded and not held.</summary>
    public string Permission { get; }
}

/// <summary>
/// Answers "may the current operator do this?" for the service layer.
/// <para>
/// Route and component attributes decide what a user can <em>reach</em>. This
/// decides what they can <em>do</em>, and it is checked inside the service that
/// performs the work. That separation is deliberate: a service method is the
/// last common point before the database, so a caller that arrives by an
/// unexpected route — a hand-made HTTP request, a component rendered through a
/// path nobody anticipated, a future endpoint someone forgets to decorate —
/// still meets the same check.
/// </para>
/// </summary>
public interface IPermissionGuard
{
    /// <summary>True when the current operator holds the permission.</summary>
    Task<bool> HasAsync(string permission, CancellationToken ct = default);

    /// <summary>True when the operator holds every one of the permissions.</summary>
    Task<bool> HasAllAsync(IEnumerable<string> permissions, CancellationToken ct = default);

    /// <summary>True when the operator holds at least one of the permissions.</summary>
    Task<bool> HasAnyAsync(IEnumerable<string> permissions, CancellationToken ct = default);

    /// <summary>Throws <see cref="ForbiddenException"/> unless the permission is held.</summary>
    Task DemandAsync(string permission, CancellationToken ct = default);

    /// <summary>Every permission the current operator holds.</summary>
    Task<IReadOnlySet<string>> CurrentAsync(CancellationToken ct = default);
}
