using DentalSurgery.Application.Abstractions;
using DentalSurgery.Infrastructure.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace DentalSurgery.Web.Api;

/// <summary>Electronic claim generation and transmission.</summary>
[ApiController]
[Route("api/claims")]
[Authorize(Policy = Permissions.InsuranceView)]
[Produces("application/json")]
public class ClaimSubmissionController(
    ClaimSubmissionService submissions,
    ILogger<ClaimSubmissionController> logger) : ControllerBase
{
    /// <summary>Validates the claim and shows what would be transmitted.</summary>
    [HttpGet("{id:guid}/preview")]
    [Authorize(Policy = Permissions.InsuranceView)]
    public async Task<IActionResult> Preview(Guid id, CancellationToken ct)
    {
        var result = await submissions.PrepareAsync(id, ct);

        if (result.Failed)
            return NotFound(new ProblemDetails { Title = result.ErrorMessage, Status = 404 });

        var preview = result.Value!;

        return Ok(new
        {
            preview.ClaimNumber,
            preview.CanSubmit,
            preview.GatewayName,
            preview.GatewayConfigured,
            preview.SegmentCount,
            Issues = preview.Issues.Select(i => new { i.Field, i.Message, i.IsBlocking }),
            Interchange = preview.Interchange
        });
    }

    /// <summary>Downloads the 837D interchange without transmitting it.</summary>
    [HttpGet("{id:guid}/interchange")]
    [Authorize(Policy = Permissions.InsuranceView)]
    [Produces("application/edi-x12")]
    public async Task<IActionResult> Interchange(Guid id, CancellationToken ct)
    {
        var result = await submissions.DownloadInterchangeAsync(id, ct);

        if (result.Failed)
            return BadRequest(new ProblemDetails
            {
                Title = "The claim cannot be generated.",
                Detail = result.ErrorMessage,
                Status = 400
            });

        return File(result.Value!, "application/edi-x12", $"claim-{id:N}.837");
    }

    /// <summary>Transmits the claim through the configured gateway.</summary>
    [HttpPost("{id:guid}/submit")]
    [Authorize(Policy = Permissions.InsuranceSubmitClaim)]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
    {
        var result = await submissions.SubmitAsync(id, ct);

        if (result.Failed)
            return UnprocessableEntity(new ProblemDetails
            {
                Title = "The claim could not be submitted.",
                Detail = result.ErrorMessage,
                Status = 422
            });

        logger.LogInformation("User {User} submitted claim {Id}.", User.Identity?.Name, id);

        return Ok(new
        {
            result.Value!.Accepted,
            result.Value.Method,
            result.Value.Reference
        });
    }

    /// <summary>Submits several claims, reporting each outcome separately.</summary>
    [HttpPost("submit-batch")]
    [Authorize(Policy = Permissions.InsuranceSubmitClaim)]
    public async Task<IActionResult> SubmitBatch([FromBody] Guid[] claimIds, CancellationToken ct)
    {
        if (claimIds.Length == 0)
            return BadRequest(new ProblemDetails { Title = "No claims were supplied.", Status = 400 });

        if (claimIds.Length > 100)
            return BadRequest(new ProblemDetails { Title = "Submit at most 100 claims at a time.", Status = 400 });

        var outcomes = await submissions.SubmitBatchAsync(claimIds, ct);

        return Ok(new
        {
            Submitted = outcomes.Count(o => o.Accepted),
            Failed = outcomes.Count(o => !o.Accepted),
            Results = outcomes.Select(o => new { o.ClaimNumber, o.Accepted, o.Error })
        });
    }
}
