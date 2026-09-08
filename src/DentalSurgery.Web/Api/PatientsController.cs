using DentalSurgery.Application.Abstractions;
using DentalSurgery.Application.Clinical;
using DentalSurgery.Application.Common;
using DentalSurgery.Domain.Common;
using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;
using DentalSurgery.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalSurgery.Web.Api;

/// <summary>Read and write access to patient records.</summary>
[ApiController]
[Route("api/patients")]
[Authorize(Policy = Permissions.PatientsView)]
[Produces("application/json")]
public class PatientsController(
    PatientService patients,
    ClinicalService clinical,
    BillingService billing) : ControllerBase
{
    /// <summary>Searches the patient register.</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.PatientsView)]
    [ProducesResponseType(typeof(PagedResult<PatientListItem>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PatientListItem>>> Search(
        [FromQuery] string? search,
        [FromQuery] PatientStatus? status = PatientStatus.Active,
        [FromQuery] bool recallDue = false,
        [FromQuery] bool withBalance = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await patients.SearchAsync(new PatientSearchCriteria
        {
            SearchText = search,
            Status = status,
            OnlyRecallDue = recallDue,
            OnlyWithBalance = withBalance,
            Page = page,
            PageSize = pageSize
        }, ct);

        return Ok(result);
    }

    /// <summary>Returns a single patient with the clinical summary shown on the record header.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.PatientsView)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var summary = await patients.GetSummaryAsync(id, ct);
        if (summary is null) return NotFound(new ProblemDetails { Title = "Patient not found", Status = 404 });

        return Ok(new
        {
            summary.Patient.Id,
            summary.Patient.PatientNumber,
            Name = summary.Patient.Name.Display,
            summary.Patient.DateOfBirth,
            Age = summary.Patient.AgeYears,
            Gender = summary.Patient.Gender.ToString(),
            Status = summary.Patient.Status.ToString(),
            Phone = summary.Patient.Contact.BestPhone,
            summary.Patient.Contact.Email,
            Address = summary.Patient.Address.ToSingleLine(),
            summary.Patient.NextRecallDue,
            Balance = summary.OutstandingBalance,
            Provider = summary.Patient.PrimaryProvider?.DisplayName,
            Risk = new
            {
                Level = summary.Risk.OverallLevel.ToString(),
                summary.Risk.RequiresAntibioticProphylaxis,
                summary.Risk.BleedingRisk,
                summary.Risk.MronjRisk,
                summary.Risk.LatexAllergy,
                Asa = summary.Risk.EstimatedAsa.ToString(),
                Flags = summary.Risk.Flags.Select(f => new { f.Code, f.Title, f.Detail, Level = f.Level.ToString() })
            },
            summary.MedicalHistoryOutOfDate,
            summary.ProcedureCount,
            summary.OpenTreatmentItems
        });
    }

    /// <summary>Registers a new patient.</summary>
    [HttpPost]
    [Authorize(Policy = Permissions.PatientsCreate)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreatePatientRequest request, CancellationToken ct)
    {
        var patient = new Patient
        {
            Name = new PersonName
            {
                Title = request.Title,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PreferredName = request.PreferredName
            },
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender ?? Gender.Unknown,
            Contact = new ContactDetails
            {
                MobilePhone = request.MobilePhone,
                HomePhone = request.HomePhone,
                Email = request.Email
            },
            Address = new Address
            {
                Line1 = request.AddressLine1,
                City = request.City,
                PostCode = request.PostCode
            },
            RecallIntervalMonths = request.RecallIntervalMonths ?? 6,
            ReferralSource = request.ReferralSource
        };

        var result = await patients.CreateAsync(patient, ct);
        if (result.Failed) return BadRequest(new ValidationProblemDetails { Title = result.ErrorMessage, Status = 400 });

        return CreatedAtAction(nameof(Get), new { id = result.Value!.Id },
            new { result.Value.Id, result.Value.PatientNumber });
    }

    /// <summary>Returns the patient's odontogram as tooth states.</summary>
    [HttpGet("{id:guid}/chart")]
    [Authorize(Policy = Permissions.DentalChartView)]
    public async Task<IActionResult> Chart(Guid id, [FromQuery] Dentition dentition = Dentition.Permanent,
        CancellationToken ct = default)
    {
        var chart = await clinical.GetChartAsync(id, dentition, null, ct);

        return Ok(new
        {
            chart.Dmft,
            chart.PresentCount,
            chart.MissingCount,
            chart.CariousCount,
            chart.RestoredCount,
            chart.ImplantCount,
            Teeth = chart.AllTeeth.Select(t => new
            {
                Fdi = t.Tooth.FdiNumber,
                t.Tooth.UniversalNumber,
                t.Tooth.PalmerNotation,
                t.Tooth.Name,
                Arch = t.Tooth.Arch.ToString(),
                t.IsMissing,
                t.IsImplant,
                t.IsCrowned,
                t.IsRootTreated,
                t.HasCaries,
                t.HasRestoration,
                t.HasPlannedWork,
                Summary = t.StatusSummary,
                RestoredSurfaces = SurfaceNotation.ToCode(t.RestoredSurfaces),
                CariousSurfaces = SurfaceNotation.ToCode(t.CariousSurfaces)
            })
        });
    }

    /// <summary>Returns the patient's ledger, invoices and ageing.</summary>
    [HttpGet("{id:guid}/account")]
    [Authorize(Policy = Permissions.BillingView)]
    public async Task<IActionResult> Account(Guid id, CancellationToken ct)
    {
        var account = await billing.GetAccountAsync(id, ct);

        return Ok(new
        {
            account.Balance,
            account.TotalCharged,
            account.TotalPaid,
            account.InsurancePending,
            Aging = new
            {
                account.Aging.Current,
                account.Aging.Days1To30,
                account.Aging.Days31To60,
                account.Aging.Days61To90,
                account.Aging.Over90Days,
                account.Aging.Total
            },
            Invoices = account.Invoices.Select(i => new
            {
                i.InvoiceNumber, i.IssueDate, i.DueDate, i.Total, i.AmountPaid, i.Balance,
                Status = i.Status.ToString()
            })
        });
    }

    /// <summary>Returns the pre-treatment risk assessment.</summary>
    [HttpGet("{id:guid}/risk")]
    [Authorize(Policy = Permissions.MedicalHistoryView)]
    public async Task<ActionResult<MedicalRiskProfile>> Risk(Guid id, CancellationToken ct)
    {
        var profile = await patients.GetRiskProfileAsync(id, ct);
        return Ok(new
        {
            Level = profile.OverallLevel.ToString(),
            Asa = profile.EstimatedAsa.ToString(),
            profile.RequiresAntibioticProphylaxis,
            profile.BleedingRisk,
            profile.AdrenalineCaution,
            profile.SedationCaution,
            profile.DelayedHealingRisk,
            profile.MronjRisk,
            profile.LatexAllergy,
            Flags = profile.Flags.Select(f => new { f.Code, f.Title, f.Detail, Level = f.Level.ToString(), f.Category })
        });
    }
}

/// <summary>Payload for registering a patient through the API.</summary>
public class CreatePatientRequest
{
    public string? Title { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PreferredName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? MobilePhone { get; set; }
    public string? HomePhone { get; set; }
    public string? Email { get; set; }
    public string? AddressLine1 { get; set; }
    public string? City { get; set; }
    public string? PostCode { get; set; }
    public int? RecallIntervalMonths { get; set; }
    public string? ReferralSource { get; set; }
}
