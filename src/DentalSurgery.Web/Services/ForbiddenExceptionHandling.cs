using DentalSurgery.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DentalSurgery.Web.Services;

/// <summary>
/// Turns a service-layer refusal into a 403 for API callers.
/// <para>
/// The guard throws rather than returning a failure result, because a caller
/// that was never entitled to the operation should not get a value it can
/// mistake for a business outcome. That decision has to be paid for here: an
/// unhandled exception would otherwise become a 500, which tells an attacker
/// their request reached real code and tells an operator nothing useful.
/// </para>
/// </summary>
public class ForbiddenExceptionFilter(ILogger<ForbiddenExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not ForbiddenException forbidden) return;

        logger.LogWarning(
            "Refused {Method} {Path} for {User}: missing {Permission}.",
            context.HttpContext.Request.Method,
            context.HttpContext.Request.Path,
            context.HttpContext.User.Identity?.Name ?? "(anonymous)",
            forbidden.Permission);

        // The permission name is safe to return: it names a capability, not a
        // record, and it is what lets an administrator grant the right thing.
        context.Result = new ObjectResult(new ProblemDetails
        {
            Title = "Not permitted",
            Detail = "This account does not hold the permission required for this operation.",
            Status = StatusCodes.Status403Forbidden,
            Extensions = { ["requiredPermission"] = forbidden.Permission }
        })
        {
            StatusCode = StatusCodes.Status403Forbidden
        };

        context.ExceptionHandled = true;
    }
}
