using DentalSurgery.Application.Abstractions;
using DentalSurgery.Application.Clinical;
using DentalSurgery.Application.Common;
using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;
using DentalSurgery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalSurgery.Infrastructure.Services;

/// <summary>Charting, clinical notes, periodontal assessment and prescribing.</summary>
public class ClinicalService(
    DentalDbContext db,
    INumberSequenceService sequences,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    ILogger<ClinicalService> logger,
    IPermissionGuard guard)
{
    private readonly DentalChartBuilder _chartBuilder = new();
    private readonly PeriodontalAnalyser _perioAnalyser = new();

    // ------------------------------------------------------------------ odontogram

    public async Task<DentalChart> GetChartAsync(
        Guid patientId, Dentition dentition = Dentition.Permanent,
        DateOnly? asOf = null, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.DentalChartView, ct);
        var teeth = await db.Teeth.AsNoTracking().OrderBy(t => t.ChartOrder).ToListAsync(ct);

        var records = await db.ToothConditionRecords.AsNoTracking()
            .Include(r => r.Tooth)
            .Include(r => r.RecordedByStaff)
            .Where(r => r.PatientId == patientId)
            .ToListAsync(ct);

        return _chartBuilder.Build(patientId, teeth, records, dentition, asOf);
    }

    public async Task<List<ToothConditionRecord>> GetToothHistoryAsync(
        Guid patientId, Guid toothId, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.DentalChartView, ct);
        return await db.ToothConditionRecords.AsNoTracking()
            .Include(r => r.RecordedByStaff)
            .Include(r => r.Procedure).ThenInclude(p => p!.ProcedureCode)
            .Where(r => r.PatientId == patientId && r.ToothId == toothId)
            .OrderByDescending(r => r.RecordedOn)
            .ThenByDescending(r => r.CreatedAtUtc)
            .ToListAsync(ct);
    }

    public async Task<Result<ToothConditionRecord>> RecordToothConditionAsync(
        ToothConditionRecord record, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.DentalChartEdit, ct);
        var tooth = await db.Teeth.AsNoTracking().FirstOrDefaultAsync(t => t.Id == record.ToothId, ct);
        if (tooth is null) return Result<ToothConditionRecord>.Failure("Tooth not found.");

        // Surfaces must be ones this tooth actually has.
        if (record.Surfaces != ToothSurface.None && record.Surfaces != ToothSurface.Whole)
        {
            var invalid = record.Surfaces & ~tooth.ValidSurfaces;
            if (invalid != ToothSurface.None)
                return Result<ToothConditionRecord>.Failure(
                    $"Surface {SurfaceNotation.ToCode(invalid)} does not exist on tooth {tooth.FdiNumber}.");
        }

        var needsSurfaces = record.ConditionType is
            ToothConditionType.Caries or ToothConditionType.Restoration or
            ToothConditionType.Inlay or ToothConditionType.Onlay or ToothConditionType.Sealant;

        if (needsSurfaces && record.Surfaces == ToothSurface.None)
            return Result<ToothConditionRecord>.Failure(
                $"At least one surface must be selected for a {record.ConditionType} entry.");

        // A tooth already recorded as missing cannot receive new findings.
        var missing = await db.ToothConditionRecords.AnyAsync(r =>
            r.PatientId == record.PatientId && r.ToothId == record.ToothId && r.IsActive &&
            (r.Status == ChartEntryStatus.Existing || r.Status == ChartEntryStatus.Completed) &&
            (r.ConditionType == ToothConditionType.Missing || r.ConditionType == ToothConditionType.Extracted), ct);

        var allowedOnMissing = record.ConditionType is
            ToothConditionType.Implant or ToothConditionType.ImplantCrown or
            ToothConditionType.BridgePontic or ToothConditionType.Denture or
            ToothConditionType.PartialDenture or ToothConditionType.SpaceMaintainer;

        if (missing && !allowedOnMissing)
            return Result<ToothConditionRecord>.Failure(
                $"Tooth {tooth.FdiNumber} is recorded as missing. Only a replacement can be charted on it.");

        // Supersede an existing finding of the same type on the same surfaces.
        var previous = await db.ToothConditionRecords
            .Where(r => r.PatientId == record.PatientId && r.ToothId == record.ToothId &&
                        r.ConditionType == record.ConditionType && r.Surfaces == record.Surfaces &&
                        r.IsActive && r.Id != record.Id)
            .OrderByDescending(r => r.RecordedOn)
            .FirstOrDefaultAsync(ct);

        if (previous is not null)
        {
            previous.IsActive = false;
            record.SupersedesId = previous.Id;
        }

        record.RecordedByStaffId ??= currentUser.StaffId;
        if (record.RecordedOn == default) record.RecordedOn = clock.Today;

        db.ToothConditionRecords.Add(record);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Charted {Condition} on tooth {Fdi} for patient {PatientId}.",
            record.ConditionType, tooth.FdiNumber, record.PatientId);

        return Result<ToothConditionRecord>.Success(record);
    }

    public async Task<Result> VoidToothConditionAsync(Guid recordId, string reason, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.DentalChartEdit, ct);
        var record = await db.ToothConditionRecords.FirstOrDefaultAsync(r => r.Id == recordId, ct);
        if (record is null) return Result.Failure("Chart entry not found.");

        record.Status = ChartEntryStatus.Voided;
        record.IsActive = false;
        record.Notes = string.IsNullOrWhiteSpace(record.Notes)
            ? $"Voided: {reason}"
            : $"{record.Notes}\nVoided: {reason}";

        // Restore whatever this entry had superseded.
        if (record.SupersedesId is { } previousId)
        {
            var previous = await db.ToothConditionRecords.FirstOrDefaultAsync(r => r.Id == previousId, ct);
            if (previous is not null) previous.IsActive = true;
        }

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ------------------------------------------------------------------ periodontal

    public async Task<List<PeriodontalChart>> GetPerioChartsAsync(Guid patientId, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.PeriodontalView, ct);
        return await db.PeriodontalCharts.AsNoTracking()
            .Include(c => c.ExaminerStaff)
            .Where(c => c.PatientId == patientId)
            .OrderByDescending(c => c.ExamDate)
            .ToListAsync(ct);
    }

    public async Task<PeriodontalChart?> GetPerioChartAsync(Guid chartId, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.PeriodontalView, ct);
        return await db.PeriodontalCharts
            .Include(c => c.Measurements).ThenInclude(m => m.Tooth)
            .Include(c => c.ExaminerStaff)
            .FirstOrDefaultAsync(c => c.Id == chartId, ct);
    }

    /// <summary>Creates a chart pre-populated with six sites for every present tooth.</summary>
    public async Task<Result<PeriodontalChart>> StartPerioChartAsync(
        Guid patientId, Guid? examinerStaffId = null, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.PeriodontalEdit, ct);
        var chart = await GetChartAsync(patientId, Dentition.Permanent, null, ct);

        var present = chart.AllTeeth.Where(t => !t.IsMissing).ToList();
        if (present.Count == 0) return Result<PeriodontalChart>.Failure("No teeth are charted as present.");

        var perio = new PeriodontalChart
        {
            PatientId = patientId,
            ExamDate = clock.Today,
            ExaminerStaffId = examinerStaffId ?? currentUser.StaffId,
            IsFullMouth = true
        };

        var sites = Enum.GetValues<PeriodontalSite>();
        foreach (var tooth in present)
        {
            foreach (var site in sites)
            {
                perio.Measurements.Add(new PeriodontalMeasurement
                {
                    PeriodontalChartId = perio.Id,
                    ToothId = tooth.Tooth.Id,
                    Site = site,
                    PocketDepthMm = 2,
                    GingivalMarginMm = 0,
                    IsImplantSite = tooth.IsImplant
                });
            }
        }

        db.PeriodontalCharts.Add(perio);
        await db.SaveChangesAsync(ct);
        return Result<PeriodontalChart>.Success(perio);
    }

    public async Task<Result<PeriodontalSummary>> SavePerioChartAsync(
        PeriodontalChart chart, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.PeriodontalEdit, ct);
        var patient = await db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == chart.PatientId, ct);
        var social = await db.SocialHistories.AsNoTracking()
            .Where(s => s.PatientId == chart.PatientId)
            .OrderByDescending(s => s.RecordedOn).FirstOrDefaultAsync(ct);

        var isSmoker = social?.SmokingStatus is SmokingStatus.Light or SmokingStatus.Moderate
            or SmokingStatus.Heavy or SmokingStatus.Occasional;

        var isDiabetic = await db.PatientMedicalConditions.AnyAsync(c =>
            c.PatientId == chart.PatientId &&
            c.MedicalCondition != null &&
            (c.MedicalCondition.Code == "DM1" || c.MedicalCondition.Code == "DM2"), ct);

        var summary = _perioAnalyser.Analyse(chart, patient?.AgeYears, isSmoker, isDiabetic);

        chart.Diagnosis = summary.SuggestedDiagnosis;
        chart.BleedingOnProbingPercent = summary.BleedingPercent;
        chart.PlaqueScorePercent = summary.PlaquePercent;
        chart.NextReviewDue = clock.Today.AddMonths(summary.RecommendedRecallMonths);
        chart.TreatmentRecommendation = string.Join(" ", summary.Recommendations);

        WriteBpeCodes(chart);

        db.PeriodontalCharts.Update(chart);
        await db.SaveChangesAsync(ct);

        return Result<PeriodontalSummary>.Success(summary);
    }

    /// <summary>Derives the six BPE sextant codes from the recorded sites.</summary>
    private static void WriteBpeCodes(PeriodontalChart chart)
    {
        var bySextant = chart.Measurements
            .Where(m => m.Tooth is not null)
            .GroupBy(m => Sextant(m.Tooth!.FdiNumber))
            .ToDictionary(g => g.Key, g => g.ToList());

        string Code(string sextant) =>
            bySextant.TryGetValue(sextant, out var sites) ? PeriodontalAnalyser.BpeCode(sites) : "X";

        chart.BpeUpperRight = Code("UR");
        chart.BpeUpperAnterior = Code("UA");
        chart.BpeUpperLeft = Code("UL");
        chart.BpeLowerRight = Code("LR");
        chart.BpeLowerAnterior = Code("LA");
        chart.BpeLowerLeft = Code("LL");
    }

    /// <summary>Maps an FDI number to its BPE sextant.</summary>
    public static string Sextant(int fdi)
    {
        var quadrant = fdi / 10;
        var position = fdi % 10;
        var anterior = position <= 3;

        return quadrant switch
        {
            1 => anterior ? "UA" : "UR",
            2 => anterior ? "UA" : "UL",
            3 => anterior ? "LA" : "LL",
            4 => anterior ? "LA" : "LR",
            _ => "UA"
        };
    }

    public Task<PeriodontalSummary> AnalysePerioAsync(PeriodontalChart chart, int? age, bool smoker, bool diabetic) =>
        Task.FromResult(_perioAnalyser.Analyse(chart, age, smoker, diabetic));

    // ------------------------------------------------------------------ clinical notes

    public async Task<List<ClinicalNote>> GetNotesAsync(Guid patientId, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.ClinicalRecordsView, ct);
        return await db.ClinicalNotes.AsNoTracking()
            .Include(n => n.Provider)
            .Include(n => n.Addenda)
            .Where(n => n.PatientId == patientId)
            .OrderByDescending(n => n.NoteDateUtc)
            .ToListAsync(ct);
    }

    public async Task<ClinicalNote?> GetNoteAsync(Guid noteId, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.ClinicalRecordsView, ct);
        return await db.ClinicalNotes
            .Include(n => n.Provider)
            .Include(n => n.Addenda)
            .Include(n => n.Appointment)
            .FirstOrDefaultAsync(n => n.Id == noteId, ct);
    }

    /// <summary>
    /// Writes or updates a clinical note, and signs it when asked.
    /// <para>
    /// Three separate permissions, because they are three separate acts.
    /// Writing a note is not the same as correcting someone else's draft, and
    /// neither is the same as signing — signing is what turns a draft into a
    /// finalised clinical record that can only be amended afterwards. A single
    /// blanket check here would let anyone who can start a note also put their
    /// signature on one.
    /// </para>
    /// </summary>
    public async Task<Result<ClinicalNote>> SaveNoteAsync(ClinicalNote note, bool sign, CancellationToken ct = default)
    {
        var existing = note.Id != Guid.Empty
            ? await db.ClinicalNotes.FirstOrDefaultAsync(n => n.Id == note.Id, ct)
            : null;

        await guard.DemandAsync(
            existing is null ? Permissions.ClinicalRecordsCreate : Permissions.ClinicalRecordsEdit, ct);

        if (sign) await guard.DemandAsync(Permissions.ClinicalRecordsSign, ct);

        if (existing is not null && existing.IsSigned)
            return Result<ClinicalNote>.Failure(
                "This note has been signed and cannot be edited. Add an addendum instead.");

        var hasContent =
            !string.IsNullOrWhiteSpace(note.Body) ||
            !string.IsNullOrWhiteSpace(note.Subjective) ||
            !string.IsNullOrWhiteSpace(note.Objective) ||
            !string.IsNullOrWhiteSpace(note.Assessment) ||
            !string.IsNullOrWhiteSpace(note.Plan) ||
            !string.IsNullOrWhiteSpace(note.TreatmentProvided);

        if (!hasContent) return Result<ClinicalNote>.Failure("The note is empty.");

        note.ProviderId ??= currentUser.StaffId;

        if (sign)
        {
            note.IsSigned = true;
            note.SignedAtUtc = clock.UtcNow;
            note.SignedBy = currentUser.DisplayName ?? currentUser.UserName;
            note.SignatureHash = ComputeSignatureHash(note);
        }

        if (existing is null) db.ClinicalNotes.Add(note);
        else db.Entry(existing).CurrentValues.SetValues(note);

        await db.SaveChangesAsync(ct);
        return Result<ClinicalNote>.Success(note);
    }

    public async Task<Result> AddAddendumAsync(
        Guid noteId, string body, string? reason, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.ClinicalRecordsEdit, ct);
        if (string.IsNullOrWhiteSpace(body)) return Result.Failure("The addendum is empty.");

        var note = await db.ClinicalNotes.FirstOrDefaultAsync(n => n.Id == noteId, ct);
        if (note is null) return Result.Failure("Note not found.");

        db.ClinicalNoteAddenda.Add(new ClinicalNoteAddendum
        {
            ClinicalNoteId = noteId,
            Body = body,
            Reason = reason,
            AddedAtUtc = clock.UtcNow,
            AddedBy = currentUser.DisplayName ?? currentUser.UserName,
            AuthorStaffId = currentUser.StaffId
        });

        note.IsAmended = true;
        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <summary>
    /// A tamper-evident digest of the signed content. Comparing it later shows
    /// whether a stored note has been altered outside the application.
    /// </summary>
    private static string ComputeSignatureHash(ClinicalNote note)
    {
        var payload = string.Join("|",
            note.Id, note.PatientId, note.NoteDateUtc.ToString("O"), note.NoteType,
            note.ChiefComplaint, note.Subjective, note.Objective, note.Assessment,
            note.Plan, note.Body, note.TreatmentProvided, note.SignedBy, note.SignedAtUtc?.ToString("O"));

        var bytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes);
    }

    // ------------------------------------------------------------------ medical history

    public async Task<Result<MedicalHistoryReview>> RecordMedicalReviewAsync(
        MedicalHistoryReview review, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.MedicalHistoryEdit, ct);
        review.ReviewedByStaffId ??= currentUser.StaffId;
        if (review.ReviewDate == default) review.ReviewDate = clock.Today;

        db.MedicalHistoryReviews.Add(review);

        // Reviews that flag a systemic risk raise a banner on the patient record.
        var flags = new List<(bool Raise, string Title, AlertSeverity Severity)>
        {
            (review.RequiresAntibioticProphylaxis, "Antibiotic prophylaxis required", AlertSeverity.High),
            (review.TakingAnticoagulants, "On anticoagulant therapy", AlertSeverity.High),
            (review.TakingBisphosphonates, "On antiresorptive therapy - MRONJ risk", AlertSeverity.High),
            (review.IsPregnant, "Pregnant", AlertSeverity.High),
            (review.HasPacemaker, "Cardiac device fitted", AlertSeverity.High),
            (review.HistoryOfRadiotherapyToHeadOrNeck, "Head and neck radiotherapy", AlertSeverity.Critical)
        };

        foreach (var (raise, title, severity) in flags.Where(f => f.Raise))
        {
            var already = await db.PatientAlerts.AnyAsync(
                a => a.PatientId == review.PatientId && a.Title == title && a.IsActive, ct);
            if (already) continue;

            db.PatientAlerts.Add(new PatientAlert
            {
                PatientId = review.PatientId,
                Category = AlertCategory.Medical,
                Severity = severity,
                Title = title,
                Detail = $"Raised automatically from the medical history review of {review.ReviewDate:d MMM yyyy}.",
                RaisedBy = currentUser.DisplayName ?? "system"
            });
        }

        await db.SaveChangesAsync(ct);
        return Result<MedicalHistoryReview>.Success(review);
    }

    // ------------------------------------------------------------------ prescribing

    /// <summary>
    /// Checks a proposed prescription against recorded allergies and interactions.
    /// Returns the warnings; the prescriber decides whether to proceed.
    /// </summary>
    public async Task<IReadOnlyList<string>> CheckPrescriptionSafetyAsync(
        Guid patientId, IEnumerable<Guid> medicationIds, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.PrescriptionsView, ct);
        var warnings = new List<string>();
        var ids = medicationIds.ToList();
        if (ids.Count == 0) return warnings;

        var medications = await db.Medications.AsNoTracking().Where(m => ids.Contains(m.Id)).ToListAsync(ct);

        var allergies = await db.PatientAllergies.AsNoTracking()
            .Include(a => a.Allergen)
            .Where(a => a.PatientId == patientId && a.IsActive)
            .ToListAsync(ct);

        foreach (var medication in medications)
        {
            var names = new[] { medication.Name, medication.GenericName, medication.DrugClass }
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n!.ToLowerInvariant())
                .ToList();

            foreach (var allergy in allergies)
            {
                var allergen = allergy.DisplayName.ToLowerInvariant();
                var cross = allergy.Allergen?.CrossReactants?.ToLowerInvariant() ?? string.Empty;

                var direct = names.Any(n => n.Contains(allergen) || allergen.Contains(n.Split(' ')[0]));
                var crossReacts = names.Any(n => cross.Contains(n.Split(' ')[0]));

                if (direct || crossReacts)
                {
                    warnings.Add(
                        $"ALLERGY: {medication.Name} conflicts with the recorded {allergy.Severity} allergy " +
                        $"to {allergy.DisplayName}{(crossReacts && !direct ? " (cross-reactivity)" : "")}.");
                }
            }

            var current = await db.PatientMedications.AsNoTracking()
                .Include(m => m.Medication)
                .Where(m => m.PatientId == patientId && m.IsCurrent)
                .ToListAsync(ct);

            if (!string.IsNullOrWhiteSpace(medication.Interactions))
            {
                var interactions = medication.Interactions.ToLowerInvariant();
                foreach (var taken in current)
                {
                    var takenName = taken.DisplayName.ToLowerInvariant().Split(' ')[0];
                    if (takenName.Length >= 4 && interactions.Contains(takenName))
                    {
                        warnings.Add(
                            $"INTERACTION: {medication.Name} may interact with the patient's current {taken.DisplayName}.");
                    }
                }
            }

            var review = await db.MedicalHistoryReviews.AsNoTracking()
                .Where(r => r.PatientId == patientId).OrderByDescending(r => r.ReviewDate)
                .FirstOrDefaultAsync(ct);

            if (review?.IsPregnant == true && medication.ContraindicatedInPregnancy)
                warnings.Add($"PREGNANCY: {medication.Name} is contraindicated in pregnancy.");

            if (review?.IsBreastfeeding == true && medication.ContraindicatedInBreastfeeding)
                warnings.Add($"BREASTFEEDING: {medication.Name} is contraindicated while breastfeeding.");

            if (medication.IsControlledDrug)
                warnings.Add($"CONTROLLED DRUG: {medication.Name} is schedule {medication.ControlledSchedule}. " +
                             "Record the quantity in words and check the controlled drugs register.");
        }

        return warnings.Distinct().ToList();
    }

    public async Task<Result<Prescription>> IssuePrescriptionAsync(
        Prescription prescription, bool acknowledgeWarnings, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.PrescriptionsCreate, ct);
        if (prescription.Items.Count == 0)
            return Result<Prescription>.Failure("A prescription needs at least one item.");

        var medicationIds = prescription.Items
            .Where(i => i.MedicationId.HasValue)
            .Select(i => i.MedicationId!.Value)
            .ToList();

        var warnings = await CheckPrescriptionSafetyAsync(prescription.PatientId, medicationIds, ct);
        var blocking = warnings.Where(w => w.StartsWith("ALLERGY", StringComparison.Ordinal)).ToList();

        if (blocking.Count > 0 && !acknowledgeWarnings)
            return Result<Prescription>.Failure(blocking);

        if (string.IsNullOrWhiteSpace(prescription.PrescriptionNumber))
            prescription.PrescriptionNumber = await sequences.NextAsync(SequenceNames.Prescription, ct);

        prescription.PrescriberStaffId ??= currentUser.StaffId;
        prescription.IssueDate = clock.Today;
        prescription.ValidUntil ??= clock.Today.AddMonths(6);
        prescription.Status = PrescriptionStatus.Issued;
        prescription.AllergiesChecked = true;
        prescription.InteractionsChecked = true;
        prescription.InteractionWarnings = warnings.Count == 0 ? null : string.Join("\n", warnings);
        prescription.IsSigned = true;
        prescription.SignedAtUtc = clock.UtcNow;

        db.Prescriptions.Add(prescription);

        // Record the drugs on the patient's current medication list.
        foreach (var item in prescription.Items.Where(i => i.MedicationId.HasValue))
        {
            db.PatientMedications.Add(new PatientMedication
            {
                PatientId = prescription.PatientId,
                MedicationId = item.MedicationId,
                Dosage = item.Dosage,
                Frequency = item.Frequency,
                Route = item.Route,
                StartDate = clock.Today,
                EndDate = item.DurationDays.HasValue ? clock.Today.AddDays(item.DurationDays.Value) : null,
                IsCurrent = true,
                PrescribedBy = currentUser.DisplayName,
                Indication = prescription.Indication
            });
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Issued prescription {Number} with {Count} items.",
            prescription.PrescriptionNumber, prescription.Items.Count);

        return Result<Prescription>.Success(prescription);
    }

    public async Task<List<Prescription>> GetPrescriptionsAsync(Guid patientId, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.PrescriptionsView, ct);
        return await db.Prescriptions.AsNoTracking()
            .Include(p => p.Items).ThenInclude(i => i.Medication)
            .Include(p => p.PrescriberStaff)
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.IssueDate)
            .ToListAsync(ct);
    }
}
