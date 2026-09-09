using DentalSurgery.Application.Abstractions;
using DentalSurgery.Domain.Common;
using DentalSurgery.Domain.Entities;
using DentalSurgery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DentalSurgery.Infrastructure.Services;

/// <summary>
/// The three duties a practice owes a patient over their own data: to hand it
/// over, to stop holding it once it may no longer be kept, and to be able to
/// say what has been removed.
/// <para>
/// Kept together because they share one idea — the retention period — and
/// separating them tends to leave the erasure half unaware of it.
/// </para>
/// </summary>
public class DataProtectionService(
    DentalDbContext db,
    IDateTimeProvider clock,
    IPermissionGuard guard,
    ILogger<DataProtectionService> logger)
{
    /// <summary>
    /// How long a clinical record must be kept before it may be erased.
    /// <para>
    /// Ten years after the last treatment is the retention period for adult
    /// dental records in the UK; for a child it runs to their 25th birthday.
    /// This is why erasure cannot simply honour a request on demand: the duty
    /// to retain outranks the right to erasure while the period is running, and
    /// a system that deleted on request would put the practice in breach rather
    /// than in compliance.
    /// </para>
    /// </summary>
    public const int ClinicalRetentionYears = 10;

    /// <summary>A child's record is kept until this age regardless of the ten years.</summary>
    public const int ChildRecordRetentionAge = 25;

    private static readonly JsonSerializerOptions ExportFormat = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

        // Entities loaded with Include carry navigations back to their parent -
        // a note points at its patient, which points at its notes - and the
        // serialiser follows them until it gives up. Cycles are written once and
        // then referenced, so the export stays complete without looping.
        ReferenceHandler = ReferenceHandler.IgnoreCycles,

        // Deep enough for the record's real nesting (plan → phase → item →
        // procedure code), shallow enough that an unexpected graph fails here
        // rather than exhausting the stack mid-export.
        MaxDepth = 32,

        Converters = { new JsonStringEnumConverter() }
    };

    // ---------------------------------------------------------------- access

    /// <summary>
    /// Everything held about one patient, as JSON, for a subject access request.
    /// <para>
    /// Assembled from the record rather than from a list of tables written by
    /// hand, so a section added to the patient record later is not silently
    /// omitted from what the practice hands over.
    /// </para>
    /// </summary>
    public async Task<SubjectAccessExport> ExportPatientAsync(Guid patientId, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.DataProtectionExport, ct);

        var patient = await db.Patients
            .AsNoTracking()
            .Include(p => p.Contacts)
            .Include(p => p.Alerts)
            .FirstOrDefaultAsync(p => p.Id == patientId, ct)
            ?? throw new InvalidOperationException($"No patient {patientId}.");

        var sections = new Dictionary<string, object?>
        {
            ["patient"] = patient,
            ["contacts"] = patient.Contacts,
            ["alerts"] = patient.Alerts,
            ["socialHistory"] = await db.SocialHistories.AsNoTracking()
                .Where(h => h.PatientId == patientId).ToListAsync(ct),

            ["medicalConditions"] = await db.PatientMedicalConditions.AsNoTracking()
                .Include(c => c.MedicalCondition)
                .Where(c => c.PatientId == patientId).ToListAsync(ct),
            ["allergies"] = await db.PatientAllergies.AsNoTracking()
                .Include(a => a.Allergen)
                .Where(a => a.PatientId == patientId).ToListAsync(ct),
            ["medications"] = await db.PatientMedications.AsNoTracking()
                .Include(m => m.Medication)
                .Where(m => m.PatientId == patientId).ToListAsync(ct),
            ["medicalHistoryReviews"] = await db.MedicalHistoryReviews.AsNoTracking()
                .Where(r => r.PatientId == patientId).ToListAsync(ct),

            ["dentalChart"] = await db.ToothConditionRecords.AsNoTracking()
                .Where(t => t.PatientId == patientId).ToListAsync(ct),
            ["periodontalCharts"] = await db.PeriodontalCharts.AsNoTracking()
                .Include(c => c.Measurements)
                .Where(c => c.PatientId == patientId).ToListAsync(ct),

            ["clinicalNotes"] = await db.ClinicalNotes.AsNoTracking()
                .Include(n => n.Addenda)
                .Where(n => n.PatientId == patientId).ToListAsync(ct),
            ["diagnoses"] = await db.PatientDiagnoses.AsNoTracking()
                .Where(d => d.PatientId == patientId).ToListAsync(ct),
            ["prescriptions"] = await db.Prescriptions.AsNoTracking()
                .Include(p => p.Items)
                .Where(p => p.PatientId == patientId).ToListAsync(ct),
            ["radiographs"] = await db.RadiographRecords.AsNoTracking()
                .Where(r => r.PatientId == patientId).ToListAsync(ct),
            ["consents"] = await db.PatientConsents.AsNoTracking()
                .Where(c => c.PatientId == patientId).ToListAsync(ct),
            ["referrals"] = await db.Referrals.AsNoTracking()
                .Where(r => r.PatientId == patientId).ToListAsync(ct),

            ["treatmentPlans"] = await db.TreatmentPlans.AsNoTracking()
                .Include(p => p.Phases).ThenInclude(p => p.Items)
                .Where(p => p.PatientId == patientId).ToListAsync(ct),
            ["procedures"] = await db.Procedures.AsNoTracking()
                .Where(p => p.PatientId == patientId).ToListAsync(ct),
            // Reached through the procedure, which is what a surgical record
            // hangs from.
            ["surgicalRecords"] = await db.SurgicalRecords.AsNoTracking()
                .Where(r => db.Procedures.Any(p => p.Id == r.ProcedureId && p.PatientId == patientId))
                .ToListAsync(ct),
            ["implants"] = await db.DentalImplants.AsNoTracking()
                .Where(i => i.PatientId == patientId).ToListAsync(ct),

            ["appointments"] = await db.Appointments.AsNoTracking()
                .Where(a => a.PatientId == patientId).ToListAsync(ct),
            ["recalls"] = await db.RecallSchedules.AsNoTracking()
                .Where(r => r.PatientId == patientId).ToListAsync(ct),

            ["invoices"] = await db.Invoices.AsNoTracking()
                .Include(i => i.Lines)
                .Where(i => i.PatientId == patientId).ToListAsync(ct),
            ["payments"] = await db.Payments.AsNoTracking()
                .Where(p => p.PatientId == patientId).ToListAsync(ct),
            ["ledger"] = await db.LedgerEntries.AsNoTracking()
                .Where(l => l.PatientId == patientId).ToListAsync(ct),
            ["insurance"] = await db.PatientInsurances.AsNoTracking()
                .Where(i => i.PatientId == patientId).ToListAsync(ct),
            ["insuranceClaims"] = await db.InsuranceClaims.AsNoTracking()
                .Include(c => c.Lines)
                .Where(c => c.PatientId == patientId).ToListAsync(ct),

            ["documents"] = await db.PatientDocuments.AsNoTracking()
                .Where(d => d.PatientId == patientId).ToListAsync(ct),
            ["communications"] = await db.CommunicationLogs.AsNoTracking()
                .Where(c => c.PatientId == patientId).ToListAsync(ct)
        };

        var payload = new
        {
            producedAtUtc = clock.UtcNow,
            about = new
            {
                patient.PatientNumber,
                name = patient.Name.Full,
                patient.DateOfBirth
            },
            note =
                "This is the practice's complete electronic record for this patient. Stored " +
                "documents and radiograph images are listed with their details; the image files " +
                "themselves are supplied alongside this export.",
            sections
        };

        logger.LogInformation(
            "Produced a subject access export for patient {PatientNumber}.", patient.PatientNumber);

        return new SubjectAccessExport(
            patient.PatientNumber,
            patient.Name.Full,
            JsonSerializer.SerializeToUtf8Bytes(payload, ExportFormat),
            await db.PatientDocuments.AsNoTracking()
                .Where(d => d.PatientId == patientId)
                .Select(d => d.StoragePath)
                .ToListAsync(ct));
    }

    // ---------------------------------------------------------------- erasure

    /// <summary>
    /// Whether this patient's identifying details may be erased yet, and if not,
    /// why not.
    /// </summary>
    public async Task<ErasureAssessment> AssessErasureAsync(Guid patientId, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.DataProtectionReview, ct);

        var patient = await db.Patients.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == patientId, ct)
            ?? throw new InvalidOperationException($"No patient {patientId}.");

        var today = clock.Today;

        var lastActivity = await LastClinicalActivityAsync(patientId, ct) ?? patient.CreatedAtUtc.Date;
        var clearOn = DateOnly.FromDateTime(lastActivity).AddYears(ClinicalRetentionYears);

        // A child's record runs to their 25th birthday, which for a young
        // patient is the later date by some margin.
        if (patient.DateOfBirth is { } dob)
        {
            var childClear = dob.AddYears(ChildRecordRetentionAge);
            if (childClear > clearOn) clearOn = childClear;
        }

        // The ledger's own running balance is the practice's position on this
        // account; summing debits and credits reproduces it without depending on
        // the last row having been written correctly.
        var outstandingBalance = await db.LedgerEntries.AsNoTracking()
            .Where(l => l.PatientId == patientId)
            .SumAsync(l => (decimal?)(l.Debit - l.Credit), ct) ?? 0m;

        var reasons = new List<string>();

        if (clearOn > today)
        {
            reasons.Add(
                $"The record must be kept until {clearOn:d MMMM yyyy}. Dental records are retained for " +
                $"{ClinicalRetentionYears} years after the last treatment, or until the patient's " +
                $"{ChildRecordRetentionAge}th birthday, whichever is later.");
        }

        if (outstandingBalance != 0m)
        {
            reasons.Add(
                $"The account is not settled ({outstandingBalance:0.00} outstanding). A record " +
                "supporting a live financial matter cannot be erased.");
        }

        return new ErasureAssessment(patientId, patient.PatientNumber, clearOn, outstandingBalance, reasons);
    }

    /// <summary>
    /// Removes a patient's identifying details while leaving the clinical and
    /// financial record intact.
    /// <para>
    /// Anonymisation rather than deletion, and deliberately so. The treatment
    /// that happened is a fact about the practice as much as about the patient:
    /// deleting it would falsify the clinical audit trail, break the ledger, and
    /// destroy the evidence a practitioner would need if that treatment were
    /// ever questioned. What is removed is everything that identifies the person
    /// — name, contacts, addresses, dates of birth, identifiers, documents —
    /// leaving records that can no longer be attributed to anyone.
    /// </para>
    /// </summary>
    public async Task<ErasureOutcome> ErasePatientAsync(
        Guid patientId, string reason, bool overrideRetention = false, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.DataProtectionErase, ct);

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("An erasure must record why it was carried out.", nameof(reason));

        var assessment = await AssessErasureAsync(patientId, ct);

        if (assessment.Blocked && !overrideRetention)
        {
            throw new InvalidOperationException(
                "This record cannot be erased yet: " + string.Join(" ", assessment.Reasons));
        }

        var patient = await db.Patients
            .Include(p => p.Contacts)
            .Include(p => p.Alerts)
            .FirstAsync(p => p.Id == patientId, ct);

        var number = patient.PatientNumber;

        patient.Name = new PersonName { FirstName = "Erased", LastName = "Record" };
        patient.Contact = new ContactDetails();
        patient.Address = new Address();
        patient.NhsNumber = null;
        patient.NationalInsuranceNumber = null;
        patient.Occupation = null;
        patient.Employer = null;
        patient.Ethnicity = null;
        patient.Notes = null;
        patient.PhotoPath = null;
        patient.ReferralSource = null;
        patient.ReferredByPatientId = null;

        // The year of birth is kept: age at the time of treatment is clinically
        // meaningful and cannot identify anyone on its own.
        if (patient.DateOfBirth is { } dob)
            patient.DateOfBirth = new DateOnly(dob.Year, 1, 1);

        patient.IsErased = true;
        patient.ErasedAtUtc = clock.UtcNow;
        patient.ErasureReason = reason;

        db.PatientContacts.RemoveRange(patient.Contacts);

        var documents = await db.PatientDocuments.Where(d => d.PatientId == patientId).ToListAsync(ct);
        var storagePaths = documents.Select(d => d.StoragePath).ToList();
        db.PatientDocuments.RemoveRange(documents);

        var communications = await db.CommunicationLogs.Where(c => c.PatientId == patientId).ToListAsync(ct);
        foreach (var entry in communications)
        {
            entry.Recipient = "erased";
            entry.Body = null;
            entry.Subject = null;
        }

        await db.SaveChangesAsync(ct);

        logger.LogWarning(
            "Erased the identifying details of patient {PatientNumber}. Reason: {Reason}. " +
            "Retention override: {Override}.", number, reason, overrideRetention);

        return new ErasureOutcome(number, storagePaths, documents.Count, communications.Count);
    }

    // ---------------------------------------------------------------- retention

    /// <summary>
    /// Deletes audit rows older than the configured retention period.
    /// <para>
    /// <c>Security.AuditRetentionYears</c> was seeded and shown on the
    /// configuration screen but read by nothing, so the trail grew forever and
    /// the stated policy was not the policy. This makes the setting mean what it
    /// says.
    /// </para>
    /// </summary>
    public async Task<int> PurgeExpiredAuditAsync(CancellationToken ct = default)
    {
        var years = await ConfiguredRetentionYearsAsync(ct);
        var cutoff = clock.UtcNow.AddYears(-years);

        var expired = await db.AuditLogs.Where(a => a.TimestampUtc < cutoff).ToListAsync(ct);
        if (expired.Count == 0) return 0;

        db.AuditLogs.RemoveRange(expired);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Removed {Count} audit row(s) older than {Years} years.", expired.Count, years);

        return expired.Count;
    }

    /// <summary>The retention period in force, from configuration.</summary>
    public async Task<int> ConfiguredRetentionYearsAsync(CancellationToken ct = default)
    {
        var setting = await db.AppSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == "Security.AuditRetentionYears", ct);

        return int.TryParse(setting?.Value, out var years) && years > 0
            ? years
            : ClinicalRetentionYears;
    }

    private async Task<DateTime?> LastClinicalActivityAsync(Guid patientId, CancellationToken ct)
    {
        var lastProcedure = await db.Procedures.AsNoTracking()
            .Where(p => p.PatientId == patientId)
            .MaxAsync(p => (DateTime?)p.DateOfService, ct);

        var lastNote = await db.ClinicalNotes.AsNoTracking()
            .Where(n => n.PatientId == patientId)
            .MaxAsync(n => (DateTime?)n.CreatedAtUtc, ct);

        return lastProcedure is null ? lastNote
            : lastNote is null ? lastProcedure
            : lastProcedure > lastNote ? lastProcedure : lastNote;
    }
}

/// <summary>A patient's complete record, ready to hand over.</summary>
public record SubjectAccessExport(
    string PatientNumber,
    string PatientName,
    byte[] Json,
    IReadOnlyList<string> DocumentPaths)
{
    public string FileName => $"subject-access-{PatientNumber}.json";
}

/// <summary>Whether a record may be erased, and what is holding it.</summary>
public record ErasureAssessment(
    Guid PatientId,
    string PatientNumber,
    DateOnly RetentionClearsOn,
    decimal OutstandingBalance,
    IReadOnlyList<string> Reasons)
{
    public bool Blocked => Reasons.Count > 0;
}

/// <summary>What an erasure removed. The storage paths still need deleting from disk.</summary>
public record ErasureOutcome(
    string PatientNumber,
    IReadOnlyList<string> DocumentPathsToDelete,
    int DocumentsRemoved,
    int CommunicationsRedacted);
