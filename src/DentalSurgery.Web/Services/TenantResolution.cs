using DentalSurgery.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using System.Security.Claims;

namespace DentalSurgery.Web.Services;

/// <summary>
/// Binds the request's scope to the signed-in user's tenant.
/// <para>
/// The tenant comes from the authentication cookie, not from anything the
/// client can choose — no header, no query parameter, no route segment. A user
/// therefore cannot ask for another practice's data, because there is nowhere
/// to ask: the only input is a claim the server issued and signed.
/// </para>
/// <para>
/// Must run after the authentication middleware and before anything that
/// queries. Until it runs, the scope has no tenant and the query filters return
/// nothing, which is the correct posture for an unauthenticated request.
/// </para>
/// </summary>
public class TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var raw = context.User.FindFirstValue(DentalClaimTypes.TenantId);

        if (!Guid.TryParse(raw, out var tenantId) || tenantId == Guid.Empty)
        {
            // An authenticated session with no usable tenant is a broken
            // account, not an anonymous one. Signing it out is safer than
            // letting it continue with no tenant, where a platform-scoped code
            // path could show it everything.
            logger.LogError(
                "Signed-in user {User} has no usable tenant claim ('{Raw}'). Ending the session.",
                context.User.Identity.Name, raw);

            await context.SignOutAsync(Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme);
            context.Response.Redirect("/Account/Login");
            return;
        }

        tenantContext.Bind(tenantId);

        // Every log line for this request carries the tenant, which is what
        // makes an incident traceable to one practice.
        using (logger.BeginScope(new Dictionary<string, object> { ["TenantId"] = tenantId }))
        {
            await next(context);
        }
    }
}

public static class TenantResolutionExtensions
{
    public static IApplicationBuilder UseTenantResolution(this IApplicationBuilder app) =>
        app.UseMiddleware<TenantResolutionMiddleware>();
}

/// <summary>
/// Binds the tenant for an interactive Blazor circuit.
/// <para>
/// A circuit is not an HTTP request. Once it is established, component code runs
/// in the circuit's own dependency-injection scope and never passes through the
/// middleware pipeline again, so <see cref="TenantResolutionMiddleware"/> — which
/// binds per request — leaves that scope with no tenant at all. The first render
/// is served over HTTP and looks right; every interactive query afterwards is
/// filtered to nothing, and the application presents as an empty database rather
/// than a broken one.
/// </para>
/// <para>
/// This closes that gap by binding the circuit's scope from the same
/// authentication state the components see, at the moment the circuit opens.
/// </para>
/// </summary>
public class TenantCircuitHandler(
    TenantContext tenantContext,
    AuthenticationStateProvider authenticationStateProvider,
    ILogger<TenantCircuitHandler> logger) : CircuitHandler
{
    public override async Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        var state = await authenticationStateProvider.GetAuthenticationStateAsync();
        var user = state.User;

        if (user.Identity?.IsAuthenticated != true) return;

        var raw = user.FindFirstValue(DentalClaimTypes.TenantId);

        if (Guid.TryParse(raw, out var tenantId) && tenantId != Guid.Empty)
        {
            tenantContext.Bind(tenantId);
            return;
        }

        // Left unbound on purpose. An unbound scope sees nothing, which is the
        // safe outcome; binding to a guess would not be.
        logger.LogError(
            "Circuit for {User} has no usable tenant claim ('{Raw}'). It will see no data.",
            user.Identity.Name, raw);
    }
}
