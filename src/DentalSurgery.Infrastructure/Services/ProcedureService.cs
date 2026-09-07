using DentalSurgery.Application.Abstractions;
using DentalSurgery.Application.Billing;
using DentalSurgery.Application.Common;
using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;
using DentalSurgery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalSurgery.Infrastructure.Services;

/// <summary>Everything captured when a procedure is completed at the chair.</summary>
public class CompleteProcedureRequest
{
    public Guid PatientId { get; set; }
    public Guid ProcedureCodeId { get; set; }
    public Guid? AppointmentId { get; set; }
    public Guid? TreatmentPlanItemId { get; set; }
    public Guid? ToothId { get; set; }
    public ToothSurface Surfaces { get; set; } = ToothSurface.None;
    public Quadrant Quadrant { get; set; } = Quadrant.None;
    public DentalArch? Arch { get; set; }
    public Guid? ProviderId { get; set; }
    public Guid? AssistantId { get; set; }
    public Guid? LocationId { get; set; }
    public RestorationMaterial Material { get; set; } = RestorationMaterial.None;
    public string? ShadeReference { get; set; }
    public string? BatchNumber { get; set; }
    public decimal? FeeOverride { get; set; }
    public decimal DiscountAmount { get; set; }
    public int Quantity { get; set; } = 1;
    public string? Notes { get; set; }
    public bool CreateInvoice { get; set; } = true;

    public SurgicalRecord? SurgicalRecord { get; set; }
    public AnaesthesiaRecord? AnaesthesiaRecord { get; set; }
    public DentalImplant? Implant { get; set; }
    public List<ProcedureMaterialUsage> MaterialsUsed { get; set; } = new();
}

/// <summary>
/// Records completed treatment. Completing a procedure updates the odontogram,
/// the treatment plan, the patient ledger and, where relevant, the implant
/// registry and stock levels, all in one transaction.
/// </summary>
public class ProcedureService(
    DentalDbContext db,
    BillingService billing,
    InventoryService inventory,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    ILogger<ProcedureService> logger)
{
    private readonly FeeCalculator _fees = new();

    public Task<List<Procedure>> GetForPatientAsync(Guid patientId, CancellationToken ct = default) =>
        db.Procedures.AsNoTracking()
            .Include(p => p.ProcedureCode).Include(p => p.Tooth)
            .Include(p => p.Provider).Include(p => p.SurgicalRecord)
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.DateOfService)
            .ToListAsync(ct);

    public Task<Procedure?> GetAsync(Guid id, CancellationToken ct = default) =>
        db.Procedures
            .Include(p => p.Patient).Include(p => p.ProcedureCode).Include(p => p.Tooth)
            .Include(p => p.Provider).Include(p => p.Assistant)
            .Include(p => p.SurgicalRecord)
            .Include(p => p.AnaesthesiaRecord).ThenInclude(a => a!.Doses)
            .Include(p => p.MaterialsUsed).ThenInclude(m => m.InventoryItem)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<Result<Procedure>> CompleteAsync(
        CompleteProcedureRequest request, CancellationToken ct = default)
    {
        var code = await db.ProcedureCodes.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ProcedureCodeId, ct);
        if (code is null) return Result<Procedure>.Failure("Procedure code not found.");

        var validation = ValidateRequest(code, request);
        if (validation.Failed) return Result<Procedure>.Failure(validation.Errors);

        var consentError = await CheckConsentAsync(code, request, ct);
        if (consentError is not null) return Result<Procedure>.Failure(consentError);

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var schedule = await db.FeeSchedules.AsNoTracking().Include(f => f.Items)
                .FirstOrDefaultAsync(f => f.IsDefault && f.IsActive, ct);

            var fee = request.FeeOverride ?? _fees.ResolveFee(code, schedule);

            var procedure = new Procedure
            {
                PatientId = request.PatientId,
                ProcedureCodeId = code.Id,
                AppointmentId = request.AppointmentId,
                TreatmentPlanItemId = request.TreatmentPlanItemId,
                ToothId = request.ToothId,
                Surfaces = request.Surfaces,
                Quadrant = request.Quadrant,
                Arch = request.Arch,
                ProviderId = request.ProviderId ?? currentUser.StaffId,
                AssistantId = request.AssistantId,
                LocationId = request.LocationId,
                DateOfService = clock.UtcNow,
                StartedAtUtc = clock.UtcNow,
                CompletedAtUtc = clock.UtcNow,
                Status = ProcedureStatus.Completed,
                Quantity = Math.Max(1, request.Quantity),
                Fee = fee,
                DiscountAmount = request.DiscountAmount,
                Material = request.Material,
                ShadeReference = request.ShadeReference,
                BatchNumber = request.BatchNumber,
                Notes = request.Notes,
                IsBillable = fee > 0
            };

            db.Procedures.Add(procedure);

            // --- operative records -----------------------------------------
            if (request.SurgicalRecord is not null)
            {
                request.SurgicalRecord.ProcedureId = procedure.Id;
                request.SurgicalRecord.DurationMinutes ??=
                    request.SurgicalRecord.IncisionTimeUtc.HasValue && request.SurgicalRecord.ClosureTimeUtc.HasValue
                        ? (int)(request.SurgicalRecord.ClosureTimeUtc.Value - request.SurgicalRecord.IncisionTimeUtc.Value).TotalMinutes
                        : null;
                db.SurgicalRecords.Add(request.SurgicalRecord);
            }

            if (request.AnaesthesiaRecord is not null)
            {
                request.AnaesthesiaRecord.ProcedureId = procedure.Id;
                request.AnaesthesiaRecord.AdministeredByStaffId ??= procedure.ProviderId;
                db.AnaesthesiaRecords.Add(request.AnaesthesiaRecord);
            }

            if (request.Implant is not null)
            {
                request.Implant.PatientId = request.PatientId;
                request.Implant.PlacementProcedureId = procedure.Id;
                request.Implant.SurgeonStaffId ??= procedure.ProviderId;
                request.Implant.PlacementDate = clock.Today;
                request.Implant.NextReviewDue = clock.Today.AddMonths(3);
                if (request.Implant.ToothId == Guid.Empty && request.ToothId.HasValue)
                    request.Implant.ToothId = request.ToothId.Value;
                db.DentalImplants.Add(request.Implant);
            }

            // --- stock consumption -----------------------------------------
            foreach (var usage in request.MaterialsUsed)
            {
                usage.ProcedureId = procedure.Id;
                db.ProcedureMaterialUsages.Add(usage);
                await inventory.ConsumeAsync(usage.InventoryItemId, usage.Quantity, procedure.Id, usage.InventoryLotId, ct);
            }

            // --- charting ---------------------------------------------------
            await UpdateChartAsync(procedure, code, ct);

            // --- treatment plan ---------------------------------------------
            if (request.TreatmentPlanItemId is { } itemId)
            {
                var item = await db.TreatmentPlanItems
                    .Include(i => i.TreatmentPlanPhase)
                    .FirstOrDefaultAsync(i => i.Id == itemId, ct);

                if (item is not null)
                {
                    item.Status = TreatmentPlanItemStatus.Completed;
                    item.CompletedOn = clock.Today;
                    item.CompletedProcedureId = procedure.Id;
                }
            }

            // --- appointment linkage ----------------------------------------
            if (request.AppointmentId is { } appointmentId)
            {
                var planned = await db.AppointmentProcedures
                    .Where(p => p.AppointmentId == appointmentId &&
                                p.ProcedureCodeId == code.Id && !p.IsCompleted)
                    .FirstOrDefaultAsync(ct);

                if (planned is not null)
                {
                    planned.IsCompleted = true;
                    planned.CompletedProcedureId = procedure.Id;
                }
            }

            // --- recall -------------------------------------------------------
            if (code.GeneratesRecall && code.RecallIntervalMonths is { } months)
                await UpsertRecallAsync(request.PatientId, code, months, ct);

            await db.SaveChangesAsync(ct);

            // --- billing --------------------------------------------------------
            if (request.CreateInvoice && procedure.IsBillable)
            {
                var invoiceResult = await billing.ChargeProcedureAsync(procedure.Id, ct);
                if (invoiceResult.Failed)
                    logger.LogWarning("Procedure {Id} recorded but not billed: {Errors}",
                        procedure.Id, invoiceResult.ErrorMessage);
            }

            await transaction.CommitAsync(ct);

            logger.LogInformation("Completed {Code} for patient {PatientId} (procedure {Id}).",
                code.Code, request.PatientId, procedure.Id);

            return Result<Procedure>.Success(procedure);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            logger.LogError(ex, "Failed to record procedure {Code} for patient {PatientId}.",
                code.Code, request.PatientId);
            return Result<Procedure>.Failure($"The procedure could not be recorded: {ex.Message}");
        }
    }

    /// <summary>Translates a completed procedure into the odontogram.</summary>
    private async Task UpdateChartAsync(Procedure procedure, ProcedureCode code, CancellationToken ct)
    {
        if (procedure.ToothId is null) return;

        var conditionType = MapToCondition(code);
        if (conditionType is null) return;

        // Anything the new work replaces stops being current.
        if (conditionType is ToothConditionType.Restoration or ToothConditionType.Crown
            or ToothConditionType.Extracted or ToothConditionType.RootCanalTreated)
        {
            var superseded = await db.ToothConditionRecords
                .Where(r => r.PatientId == procedure.PatientId &&
                            r.ToothId == procedure.ToothId &&
                            r.IsActive &&
                            r.ConditionType == ToothConditionType.Caries &&
                            (r.Surfaces & procedure.Surfaces) != ToothSurface.None)
                .ToListAsync(ct);

            foreach (var record in superseded)
            {
                record.IsActive = false;
                record.Status = ChartEntryStatus.Completed;
            }
        }

        db.ToothConditionRecords.Add(new ToothConditionRecord
        {
            PatientId = procedure.PatientId,
            ToothId = procedure.ToothId.Value,
            Surfaces = procedure.Surfaces,
            ConditionType = conditionType.Value,
            Status = ChartEntryStatus.Completed,
            Material = procedure.Material,
            RecordedOn = DateOnly.FromDateTime(procedure.DateOfService),
            RecordedByStaffId = procedure.ProviderId,
            ProcedureId = procedure.Id,
            ShadeReference = procedure.ShadeReference,
            Notes = $"Recorded from {code.Code} - {code.ShortDescription}."
        });
    }

    /// <summary>Maps a procedure code to the chart symbol it produces.</summary>
    public static ToothConditionType? MapToCondition(ProcedureCode code)
    {
        var c = code.Code;

        if (c.StartsWith("D71") || c.StartsWith("D72") || c is "D7250")
            return ToothConditionType.Extracted;
        if (c is "D6010" or "D6011") return ToothConditionType.Implant;
        if (c is "D6058") return ToothConditionType.ImplantCrown;
        if (c.StartsWith("D274") || c.StartsWith("D275") || c is "D2790") return ToothConditionType.Crown;
        if (c is "D6750") return ToothConditionType.BridgeAbutment;
        if (c is "D6240") return ToothConditionType.BridgePontic;
        if (c is "D2962") return ToothConditionType.Veneer;
        if (c is "D2510" or "D2610") return ToothConditionType.Inlay;
        if (c is "D2642") return ToothConditionType.Onlay;
        if (c.StartsWith("D33") || c.StartsWith("D334")) return ToothConditionType.RootCanalTreated;
        if (c is "D2954") return ToothConditionType.PostAndCore;
        if (c is "D1351") return ToothConditionType.Sealant;
        if (c is "D2940") return ToothConditionType.TemporaryRestoration;
        if (c is "D2930") return ToothConditionType.Crown;

        return code.Category switch
        {
            ProcedureCategory.Restorative when code.RequiresSurfaces => ToothConditionType.Restoration,
            _ => null
        };
    }

    private async Task UpsertRecallAsync(Guid patientId, ProcedureCode code, int months, CancellationToken ct)
    {
        var recallType = code.IsHygieneProcedure ? RecallType.ScaleAndPolish
            : code.Category == ProcedureCategory.Radiology ? RecallType.Radiographs
            : code.Category == ProcedureCategory.ImplantServices ? RecallType.ImplantReview
            : RecallType.RoutineExam;

        var recall = await db.RecallSchedules
            .FirstOrDefaultAsync(r => r.PatientId == patientId && r.RecallType == recallType && r.IsActive, ct);

        if (recall is null)
        {
            recall = new RecallSchedule { PatientId = patientId, RecallType = recallType, IntervalMonths = months };
            db.RecallSchedules.Add(recall);
        }

        recall.LastCompletedDate = clock.Today;
        recall.DueDate = clock.Today.AddMonths(months);
        recall.Status = RecallStatus.Scheduled;
        recall.ContactAttempts = 0;
    }

    private static Result ValidateRequest(ProcedureCode code, CompleteProcedureRequest request)
    {
        var errors = new List<string>();

        if (code.RequiresTooth && request.ToothId is null)
            errors.Add($"{code.Code} requires a tooth.");
        if (code.RequiresSurfaces && request.Surfaces == ToothSurface.None)
            errors.Add($"{code.Code} requires at least one surface.");
        if (code.RequiresQuadrant && request.Quadrant == Quadrant.None)
            errors.Add($"{code.Code} requires a quadrant.");
        if (code.RequiresArch && request.Arch is null)
            errors.Add($"{code.Code} requires an arch.");
        if (code.IsSurgical && request.SurgicalRecord is null)
            errors.Add($"{code.Code} is a surgical procedure and needs an operative record.");
        if (request.Quantity < 1)
            errors.Add("The quantity must be at least one.");

        return errors.Count == 0 ? Result.Success() : Result.Failure(errors);
    }

    /// <summary>Blocks procedures that need written consent until it has been captured.</summary>
    private async Task<string?> CheckConsentAsync(
        ProcedureCode code, CompleteProcedureRequest request, CancellationToken ct)
    {
        if (!code.RequiresConsent) return null;

        var hasConsent = await db.PatientConsents
            .Include(c => c.ConsentFormTemplate)
            .AnyAsync(c => c.PatientId == request.PatientId &&
                           c.Status == ConsentStatus.Signed &&
                           (c.ExpiresOn == null || c.ExpiresOn >= clock.Today) &&
                           c.ConsentFormTemplate != null &&
                           c.ConsentFormTemplate.AppliesToProcedureCodes != null &&
                           c.ConsentFormTemplate.AppliesToProcedureCodes.Contains(code.Code), ct);

        return hasConsent
            ? null
            : $"{code.Code} requires signed consent. Capture the consent form before recording the procedure.";
    }

    public async Task<Result> VoidAsync(Guid procedureId, string reason, CancellationToken ct = default)
    {
        var procedure = await db.Procedures.FirstOrDefaultAsync(p => p.Id == procedureId, ct);
        if (procedure is null) return Result.Failure("Procedure not found.");
        if (procedure.Status == ProcedureStatus.Voided) return Result.Failure("This procedure is already voided.");

        procedure.Status = ProcedureStatus.Voided;
        procedure.Notes = string.IsNullOrWhiteSpace(procedure.Notes)
            ? $"Voided: {reason}"
            : $"{procedure.Notes}\nVoided: {reason}";

        // Retract the chart entry this procedure produced.
        var chartEntries = await db.ToothConditionRecords
            .Where(r => r.ProcedureId == procedureId).ToListAsync(ct);
        foreach (var entry in chartEntries)
        {
            entry.IsActive = false;
            entry.Status = ChartEntryStatus.Voided;
            if (entry.SupersedesId is { } previousId)
            {
                var previous = await db.ToothConditionRecords.FirstOrDefaultAsync(r => r.Id == previousId, ct);
                if (previous is not null) previous.IsActive = true;
            }
        }

        await db.SaveChangesAsync(ct);
        await billing.ReverseProcedureChargeAsync(procedureId, reason, ct);
        return Result.Success();
    }

    public Task<List<DentalImplant>> GetImplantsAsync(Guid patientId, CancellationToken ct = default) =>
        db.DentalImplants.AsNoTracking()
            .Include(i => i.Tooth).Include(i => i.SurgeonStaff)
            .Where(i => i.PatientId == patientId)
            .OrderByDescending(i => i.PlacementDate)
            .ToListAsync(ct);

    public Task<List<Procedure>> GetSurgicalCasesAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var start = from.ToDateTime(TimeOnly.MinValue);
        var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue);

        return db.Procedures.AsNoTracking()
            .Include(p => p.Patient).Include(p => p.ProcedureCode)
            .Include(p => p.Tooth).Include(p => p.Provider)
            .Include(p => p.SurgicalRecord)
            .Where(p => p.SurgicalRecord != null &&
                        p.DateOfService >= start && p.DateOfService < end &&
                        p.Status != ProcedureStatus.Voided)
            .OrderByDescending(p => p.DateOfService)
            .ToListAsync(ct);
    }
}
