using DentalSurgery.Application.Abstractions;
using DentalSurgery.Application.Clinical;
using DentalSurgery.Application.Common;
using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;
using DentalSurgery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalSurgery.Infrastructure.Services;

/// <summary>Filters applied to the patient list.</summary>
public class PatientSearchCriteria
{
    public string? SearchText { get; set; }
    public PatientStatus? Status { get; set; } = PatientStatus.Active;
    public Guid? ProviderId { get; set; }
    public Guid? LocationId { get; set; }
    public bool OnlyWithBalance { get; set; }
    public bool OnlyRecallDue { get; set; }
    public bool OnlyWithAlerts { get; set; }
    public DateOnly? RegisteredFrom { get; set; }
    public DateOnly? RegisteredTo { get; set; }
    public string SortBy { get; set; } = "Name";
    public bool SortDescending { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

/// <summary>A patient row as shown in list views.</summary>
public record PatientListItem(
    Guid Id,
    string PatientNumber,
    string DisplayName,
    string SortName,
    DateOnly? DateOfBirth,
    int? Age,
    Gender Gender,
    string? Phone,
    string? Email,
    PatientStatus Status,
    decimal Balance,
    DateOnly? NextRecallDue,
    DateOnly? LastExamDate,
    string? ProviderName,
    int ActiveAlertCount,
    int CriticalAlertCount)
{
    public bool RecallOverdue =>
        NextRecallDue.HasValue && NextRecallDue.Value < DateOnly.FromDateTime(DateTime.Today);
}

/// <summary>Everything needed to render the patient header and summary tab.</summary>
public class PatientSummary
{
    public Patient Patient { get; init; } = null!;
    public MedicalRiskProfile Risk { get; init; } = new();
    public IReadOnlyList<PatientAlert> ActiveAlerts { get; init; } = Array.Empty<PatientAlert>();
    public Appointment? NextAppointment { get; init; }
    public Appointment? LastAppointment { get; init; }
    public MedicalHistoryReview? LatestMedicalReview { get; init; }
    public PeriodontalChart? LatestPerioChart { get; init; }
    public TreatmentPlan? ActiveTreatmentPlan { get; init; }
    public decimal OutstandingBalance { get; init; }
    public decimal InsurancePending { get; init; }
    public int OpenTreatmentItems { get; init; }
    public int ProcedureCount { get; init; }
    public int RadiographCount { get; init; }
    public int DocumentCount { get; init; }
    public DateOnly? MedicalHistoryDue { get; init; }

    public bool MedicalHistoryOutOfDate =>
        LatestMedicalReview is null ||
        LatestMedicalReview.ReviewDate < DateOnly.FromDateTime(DateTime.Today.AddMonths(-12));
}

/// <summary>Registration, search and summary operations for patients.</summary>
public class PatientService(
    DentalDbContext db,
    INumberSequenceService sequences,
    IDateTimeProvider clock,
    ILogger<PatientService> logger,
    IPermissionGuard guard)
{
    private readonly MedicalRiskAssessor _riskAssessor = new();

    // ------------------------------------------------------------------ query

    public async Task<PagedResult<PatientListItem>> SearchAsync(
        PatientSearchCriteria criteria, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.PatientsView, ct);
        var query = db.Patients.AsNoTracking().AsQueryable();

        if (criteria.Status.HasValue)
            query = query.Where(p => p.Status == criteria.Status.Value);

        if (!string.IsNullOrWhiteSpace(criteria.SearchText))
        {
            var text = criteria.SearchText.Trim();
            query = query.Where(p =>
                p.PatientNumber.Contains(text) ||
                p.Name.FirstName.Contains(text) ||
                p.Name.LastName.Contains(text) ||
                (p.Name.PreferredName != null && p.Name.PreferredName.Contains(text)) ||
                (p.Contact.MobilePhone != null && p.Contact.MobilePhone.Contains(text)) ||
                (p.Contact.HomePhone != null && p.Contact.HomePhone.Contains(text)) ||
                (p.Contact.Email != null && p.Contact.Email.Contains(text)) ||
                (p.Address.PostCode != null && p.Address.PostCode.Contains(text)) ||
                (p.NhsNumber != null && p.NhsNumber.Contains(text)));
        }

        if (criteria.ProviderId.HasValue)
            query = query.Where(p => p.PrimaryProviderId == criteria.ProviderId || p.PrimaryHygienistId == criteria.ProviderId);

        if (criteria.LocationId.HasValue)
            query = query.Where(p => p.PreferredLocationId == criteria.LocationId);

        if (criteria.OnlyWithBalance)
            query = query.Where(p => p.AccountBalance > 0);

        if (criteria.OnlyRecallDue)
        {
            var today = clock.Today;
            query = query.Where(p => p.NextRecallDue != null && p.NextRecallDue <= today);
        }

        if (criteria.OnlyWithAlerts)
            query = query.Where(p => p.Alerts.Any(a => a.IsActive));

        if (criteria.RegisteredFrom.HasValue)
            query = query.Where(p => p.RegistrationDate >= criteria.RegisteredFrom.Value);

        if (criteria.RegisteredTo.HasValue)
            query = query.Where(p => p.RegistrationDate <= criteria.RegisteredTo.Value);

        query = (criteria.SortBy, criteria.SortDescending) switch
        {
            ("Number", false) => query.OrderBy(p => p.PatientNumber),
            ("Number", true) => query.OrderByDescending(p => p.PatientNumber),
            ("Balance", false) => query.OrderBy(p => p.AccountBalance),
            ("Balance", true) => query.OrderByDescending(p => p.AccountBalance),
            ("Recall", false) => query.OrderBy(p => p.NextRecallDue),
            ("Recall", true) => query.OrderByDescending(p => p.NextRecallDue),
            ("Registered", false) => query.OrderBy(p => p.RegistrationDate),
            ("Registered", true) => query.OrderByDescending(p => p.RegistrationDate),
            (_, true) => query.OrderByDescending(p => p.Name.LastName).ThenByDescending(p => p.Name.FirstName),
            _ => query.OrderBy(p => p.Name.LastName).ThenBy(p => p.Name.FirstName)
        };

        var total = await query.CountAsync(ct);
        var page = Math.Max(1, criteria.Page);
        var size = Math.Clamp(criteria.PageSize, 5, 200);

        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .Select(p => new PatientListItem(
                p.Id,
                p.PatientNumber,
                (p.Name.PreferredName ?? p.Name.FirstName) + " " + p.Name.LastName,
                p.Name.LastName + ", " + p.Name.FirstName,
                p.DateOfBirth,
                null,
                p.Gender,
                p.Contact.MobilePhone ?? p.Contact.HomePhone,
                p.Contact.Email,
                p.Status,
                p.AccountBalance,
                p.NextRecallDue,
                p.LastExamDate,
                p.PrimaryProvider != null ? p.PrimaryProvider.Name.FirstName + " " + p.PrimaryProvider.Name.LastName : null,
                p.Alerts.Count(a => a.IsActive),
                p.Alerts.Count(a => a.IsActive && a.Severity >= AlertSeverity.High)))
            .ToListAsync(ct);

        // Age is derived in memory: SQLite cannot express the birthday comparison.
        var withAge = items
            .Select(i => i with { Age = CalculateAge(i.DateOfBirth) })
            .ToList();

        return PagedResult<PatientListItem>.Create(withAge, total, page, size);
    }

    /// <summary>Fast lookup for pickers and the global search box.</summary>
    public async Task<IReadOnlyList<PatientListItem>> QuickSearchAsync(
        string text, int limit = 10, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.PatientsView, ct);
        if (string.IsNullOrWhiteSpace(text) || text.Trim().Length < 2)
            return Array.Empty<PatientListItem>();

        var criteria = new PatientSearchCriteria
        {
            SearchText = text, Status = null, PageSize = limit, Page = 1
        };

        var result = await SearchAsync(criteria, ct);
        return result.Items;
    }

    public async Task<Patient?> GetAsync(Guid id, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.PatientsView, ct);
        return await db.Patients
            .Include(p => p.PrimaryProvider)
            .Include(p => p.PrimaryHygienist)
            .Include(p => p.PreferredLocation)
            .Include(p => p.Contacts)
            .Include(p => p.Alerts)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<PatientSummary?> GetSummaryAsync(Guid id, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.PatientsView, ct);
        var patient = await db.Patients
            .Include(p => p.PrimaryProvider)
            .Include(p => p.PrimaryHygienist)
            .Include(p => p.PreferredLocation)
            .Include(p => p.Alerts)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (patient is null) return null;

        var allergies = await db.PatientAllergies.AsNoTracking()
            .Include(a => a.Allergen)
            .Where(a => a.PatientId == id && a.IsActive)
            .ToListAsync(ct);

        var conditions = await db.PatientMedicalConditions.AsNoTracking()
            .Include(c => c.MedicalCondition)
            .Where(c => c.PatientId == id)
            .ToListAsync(ct);

        var medications = await db.PatientMedications.AsNoTracking()
            .Include(m => m.Medication)
            .Where(m => m.PatientId == id && m.IsCurrent)
            .ToListAsync(ct);

        var review = await db.MedicalHistoryReviews.AsNoTracking()
            .Where(r => r.PatientId == id)
            .OrderByDescending(r => r.ReviewDate)
            .FirstOrDefaultAsync(ct);

        var now = clock.UtcNow;

        var nextAppointment = await db.Appointments.AsNoTracking()
            .Include(a => a.Provider).Include(a => a.Operatory)
            .Where(a => a.PatientId == id && a.StartUtc >= now &&
                        a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.NoShow)
            .OrderBy(a => a.StartUtc)
            .FirstOrDefaultAsync(ct);

        var lastAppointment = await db.Appointments.AsNoTracking()
            .Include(a => a.Provider)
            .Where(a => a.PatientId == id && a.StartUtc < now && a.Status == AppointmentStatus.Completed)
            .OrderByDescending(a => a.StartUtc)
            .FirstOrDefaultAsync(ct);

        var perio = await db.PeriodontalCharts.AsNoTracking()
            .Include(c => c.Measurements)
            .Where(c => c.PatientId == id)
            .OrderByDescending(c => c.ExamDate)
            .FirstOrDefaultAsync(ct);

        var plan = await db.TreatmentPlans.AsNoTracking()
            .Include(p => p.Phases).ThenInclude(ph => ph.Items)
            .Where(p => p.PatientId == id &&
                        (p.Status == TreatmentPlanStatus.Accepted ||
                         p.Status == TreatmentPlanStatus.PartiallyAccepted ||
                         p.Status == TreatmentPlanStatus.InProgress ||
                         p.Status == TreatmentPlanStatus.Presented))
            .OrderByDescending(p => p.CreatedOn)
            .FirstOrDefaultAsync(ct);

        var openItems = plan?.AllItems.Count(i =>
            i.Status is TreatmentPlanItemStatus.Accepted or TreatmentPlanItemStatus.Scheduled
                or TreatmentPlanItemStatus.InProgress) ?? 0;

        var risk = _riskAssessor.Assess(patient, allergies, conditions, medications, review);

        return new PatientSummary
        {
            Patient = patient,
            Risk = risk,
            ActiveAlerts = patient.Alerts.Where(a => a.IsActive).OrderByDescending(a => a.Severity).ToList(),
            NextAppointment = nextAppointment,
            LastAppointment = lastAppointment,
            LatestMedicalReview = review,
            LatestPerioChart = perio,
            ActiveTreatmentPlan = plan,
            OutstandingBalance = patient.AccountBalance,
            InsurancePending = patient.InsurancePending,
            OpenTreatmentItems = openItems,
            ProcedureCount = await db.Procedures.CountAsync(p => p.PatientId == id && p.Status == ProcedureStatus.Completed, ct),
            RadiographCount = await db.RadiographRecords.CountAsync(r => r.PatientId == id, ct),
            DocumentCount = await db.PatientDocuments.CountAsync(d => d.PatientId == id, ct),
            MedicalHistoryDue = review?.ReviewDate.AddMonths(12)
        };
    }

    // ------------------------------------------------------------------ commands

    public async Task<Result<Patient>> CreateAsync(Patient patient, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.PatientsCreate, ct);
        var validation = Validate(patient);
        if (validation.Failed) return Result<Patient>.Failure(validation.Errors);

        var numberWasSupplied = !string.IsNullOrWhiteSpace(patient.PatientNumber);
        if (!numberWasSupplied)
            patient.PatientNumber = await sequences.NextAsync(SequenceNames.Patient, ct);

        if (patient.RegistrationDate == default) patient.RegistrationDate = clock.Today;

        // New registrations start with a recall so nobody is lost to follow-up.
        patient.NextRecallDue ??= clock.Today.AddMonths(patient.RecallIntervalMonths);

        db.Patients.Add(patient);

        db.RecallSchedules.Add(new RecallSchedule
        {
            PatientId = patient.Id,
            RecallType = RecallType.RoutineExam,
            IntervalMonths = patient.RecallIntervalMonths,
            DueDate = patient.NextRecallDue.Value,
            Status = RecallStatus.Scheduled,
            PreferredProviderId = patient.PrimaryProviderId
        });

        // Two receptionists registering at the same moment, or a sequence left
        // behind by an import, can both produce the same number. Take the next
        // one and try again rather than failing the registration.
        const int maxAttempts = 5;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await db.SaveChangesAsync(ct);
                break;
            }
            catch (DbUpdateException ex) when (IsDuplicatePatientNumber(ex) && !numberWasSupplied && attempt < maxAttempts)
            {
                logger.LogWarning("Patient number {Number} was already taken; allocating another.", patient.PatientNumber);
                patient.PatientNumber = await sequences.NextAsync(SequenceNames.Patient, ct);
            }
            catch (DbUpdateException ex) when (IsDuplicatePatientNumber(ex))
            {
                return Result<Patient>.Failure($"Patient number {patient.PatientNumber} is already in use.");
            }
        }

        logger.LogInformation("Registered patient {Number} ({Id}).", patient.PatientNumber, patient.Id);
        return Result<Patient>.Success(patient);
    }

    private static bool IsDuplicatePatientNumber(DbUpdateException exception) =>
        exception.InnerException?.Message.Contains("Patients.PatientNumber", StringComparison.OrdinalIgnoreCase) == true;

    public async Task<Result> UpdateAsync(Patient patient, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.PatientsEdit, ct);
        var validation = Validate(patient);
        if (validation.Failed) return validation;

        db.Patients.Update(patient);
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> SetStatusAsync(
        Guid patientId, PatientStatus status, string? reason, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.PatientsEdit, ct);
        var patient = await db.Patients.FirstOrDefaultAsync(p => p.Id == patientId, ct);
        if (patient is null) return Result.Failure("Patient not found.");

        patient.Status = status;

        if (status is PatientStatus.Inactive or PatientStatus.Archived or PatientStatus.Transferred)
        {
            patient.InactiveDate = clock.Today;
            patient.InactiveReason = reason;

            // Stop chasing recalls for patients who have left.
            var recalls = await db.RecallSchedules
                .Where(r => r.PatientId == patientId && r.IsActive)
                .ToListAsync(ct);
            foreach (var recall in recalls)
            {
                recall.IsActive = false;
                recall.Status = RecallStatus.Suspended;
            }
        }
        else if (status == PatientStatus.Deceased)
        {
            patient.DateOfDeath ??= clock.Today;
            patient.AllowEmail = patient.AllowSms = patient.AllowPhoneCall = patient.AllowPost = false;
            patient.AllowMarketing = false;

            var recalls = await db.RecallSchedules.Where(r => r.PatientId == patientId).ToListAsync(ct);
            foreach (var recall in recalls) { recall.IsActive = false; recall.Status = RecallStatus.OptedOut; }
        }
        else
        {
            patient.InactiveDate = null;
            patient.InactiveReason = null;
        }

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<PatientAlert>> AddAlertAsync(PatientAlert alert, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.PatientsEdit, ct);
        if (string.IsNullOrWhiteSpace(alert.Title))
            return Result<PatientAlert>.Failure("An alert needs a title.");

        db.PatientAlerts.Add(alert);
        await db.SaveChangesAsync(ct);
        return Result<PatientAlert>.Success(alert);
    }

    public async Task<Result> DismissAlertAsync(Guid alertId, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.PatientsEdit, ct);
        var alert = await db.PatientAlerts.FirstOrDefaultAsync(a => a.Id == alertId, ct);
        if (alert is null) return Result.Failure("Alert not found.");

        alert.IsActive = false;
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<MedicalRiskProfile> GetRiskProfileAsync(Guid patientId, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.MedicalHistoryView, ct);
        var patient = await db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == patientId, ct);
        if (patient is null) return new MedicalRiskProfile();

        var allergies = await db.PatientAllergies.AsNoTracking().Include(a => a.Allergen)
            .Where(a => a.PatientId == patientId && a.IsActive).ToListAsync(ct);
        var conditions = await db.PatientMedicalConditions.AsNoTracking().Include(c => c.MedicalCondition)
            .Where(c => c.PatientId == patientId).ToListAsync(ct);
        var medications = await db.PatientMedications.AsNoTracking().Include(m => m.Medication)
            .Where(m => m.PatientId == patientId && m.IsCurrent).ToListAsync(ct);
        var review = await db.MedicalHistoryReviews.AsNoTracking()
            .Where(r => r.PatientId == patientId).OrderByDescending(r => r.ReviewDate).FirstOrDefaultAsync(ct);

        return _riskAssessor.Assess(patient, allergies, conditions, medications, review);
    }

    // ------------------------------------------------------------------ helpers

    private static Result Validate(Patient patient)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(patient.Name.FirstName)) errors.Add("A first name is required.");
        if (string.IsNullOrWhiteSpace(patient.Name.LastName)) errors.Add("A surname is required.");

        if (patient.DateOfBirth is { } dob)
        {
            if (dob > DateOnly.FromDateTime(DateTime.Today))
                errors.Add("The date of birth cannot be in the future.");
            if (dob < DateOnly.FromDateTime(DateTime.Today.AddYears(-130)))
                errors.Add("The date of birth is not plausible.");
        }

        if (!string.IsNullOrWhiteSpace(patient.Contact.Email) && !patient.Contact.Email.Contains('@'))
            errors.Add("The email address is not valid.");

        if (patient.RecallIntervalMonths is < 1 or > 60)
            errors.Add("The recall interval must be between 1 and 60 months.");

        var hasContact =
            !string.IsNullOrWhiteSpace(patient.Contact.MobilePhone) ||
            !string.IsNullOrWhiteSpace(patient.Contact.HomePhone) ||
            !string.IsNullOrWhiteSpace(patient.Contact.WorkPhone) ||
            !string.IsNullOrWhiteSpace(patient.Contact.Email);

        if (!hasContact) errors.Add("At least one phone number or email address is required.");

        return errors.Count == 0 ? Result.Success() : Result.Failure(errors);
    }

    private static int? CalculateAge(DateOnly? dateOfBirth)
    {
        if (dateOfBirth is null) return null;
        var today = DateOnly.FromDateTime(DateTime.Today);
        var age = today.Year - dateOfBirth.Value.Year;
        if (today < dateOfBirth.Value.AddYears(age)) age--;
        return age < 0 ? null : age;
    }
}
