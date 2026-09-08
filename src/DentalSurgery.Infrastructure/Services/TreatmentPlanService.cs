using DentalSurgery.Application.Abstractions;
using DentalSurgery.Application.Billing;
using DentalSurgery.Application.Common;
using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;
using DentalSurgery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalSurgery.Infrastructure.Services;

/// <summary>A procedure to add to a plan, as captured in the builder screen.</summary>
public record PlanItemRequest(
    Guid ProcedureCodeId,
    Guid? ToothId = null,
    ToothSurface Surfaces = ToothSurface.None,
    Quadrant Quadrant = Quadrant.None,
    DentalArch? Arch = null,
    int Quantity = 1,
    decimal? FeeOverride = null,
    decimal DiscountAmount = 0m,
    Guid? ProviderId = null,
    TreatmentPriority Priority = TreatmentPriority.Routine,
    string? Notes = null);

/// <summary>Builds, prices and tracks acceptance of treatment plans.</summary>
public class TreatmentPlanService(
    DentalDbContext db,
    INumberSequenceService sequences,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    ILogger<TreatmentPlanService> logger,
    IPermissionGuard guard)
{
    private readonly FeeCalculator _fees = new();
    private readonly InsuranceEstimator _estimator = new();

    public async Task<List<TreatmentPlan>> GetForPatientAsync(Guid patientId, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.TreatmentPlansView, ct);
        return await db.TreatmentPlans.AsNoTracking()
            .Include(p => p.Provider)
            .Include(p => p.Phases).ThenInclude(ph => ph.Items).ThenInclude(i => i.ProcedureCode)
            .Include(p => p.Phases).ThenInclude(ph => ph.Items).ThenInclude(i => i.Tooth)
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.CreatedOn)
            .ToListAsync(ct);
    }

    public async Task<TreatmentPlan?> GetAsync(Guid planId, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.TreatmentPlansView, ct);
        return await db.TreatmentPlans
            .Include(p => p.Patient)
            .Include(p => p.Provider)
            .Include(p => p.FeeSchedule)
            .Include(p => p.Phases.OrderBy(ph => ph.PhaseNumber))
                .ThenInclude(ph => ph.Items.OrderBy(i => i.Sequence))
                .ThenInclude(i => i.ProcedureCode)
            .Include(p => p.Phases).ThenInclude(ph => ph.Items).ThenInclude(i => i.Tooth)
            .Include(p => p.Phases).ThenInclude(ph => ph.Items).ThenInclude(i => i.Provider)
            .FirstOrDefaultAsync(p => p.Id == planId, ct);
    }

    public async Task<Result<TreatmentPlan>> CreateAsync(
        Guid patientId, string name, Guid? providerId, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.TreatmentPlansCreate, ct);
        var patient = await db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == patientId, ct);
        if (patient is null) return Result<TreatmentPlan>.Failure("Patient not found.");

        var schedule = await db.FeeSchedules.AsNoTracking()
            .FirstOrDefaultAsync(f => f.IsDefault && f.IsActive, ct);

        var plan = new TreatmentPlan
        {
            PatientId = patientId,
            PlanNumber = await sequences.NextAsync(SequenceNames.TreatmentPlan, ct),
            Name = string.IsNullOrWhiteSpace(name) ? "Treatment Plan" : name,
            ProviderId = providerId ?? patient.PrimaryProviderId ?? currentUser.StaffId,
            FeeScheduleId = schedule?.Id,
            CreatedOn = clock.Today,
            ValidUntil = clock.Today.AddDays(90),
            Status = TreatmentPlanStatus.Draft
        };

        // Every plan starts with the standard three-phase structure.
        plan.Phases.Add(new TreatmentPlanPhase
        {
            TreatmentPlanId = plan.Id, PhaseNumber = 1, Name = "Phase 1 - Stabilisation",
            ClinicalObjective = "Relieve pain, control infection and stabilise active disease.",
            Priority = TreatmentPriority.Urgent
        });
        plan.Phases.Add(new TreatmentPlanPhase
        {
            TreatmentPlanId = plan.Id, PhaseNumber = 2, Name = "Phase 2 - Restoration",
            ClinicalObjective = "Restore form and function to the affected teeth.",
            Priority = TreatmentPriority.Routine
        });
        plan.Phases.Add(new TreatmentPlanPhase
        {
            TreatmentPlanId = plan.Id, PhaseNumber = 3, Name = "Phase 3 - Maintenance",
            ClinicalObjective = "Preventive care and recall to hold the result.",
            Priority = TreatmentPriority.Monitor
        });

        db.TreatmentPlans.Add(plan);
        await db.SaveChangesAsync(ct);
        return Result<TreatmentPlan>.Success(plan);
    }

    public async Task<Result<TreatmentPlanItem>> AddItemAsync(
        Guid phaseId, PlanItemRequest request, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.TreatmentPlansEdit, ct);
        var phase = await db.TreatmentPlanPhases
            .Include(p => p.Items)
            .Include(p => p.TreatmentPlan)
            .FirstOrDefaultAsync(p => p.Id == phaseId, ct);

        if (phase is null) return Result<TreatmentPlanItem>.Failure("Plan phase not found.");
        if (phase.TreatmentPlan!.Status is TreatmentPlanStatus.Completed or TreatmentPlanStatus.Superseded)
            return Result<TreatmentPlanItem>.Failure("This plan is closed and cannot be changed.");

        var code = await db.ProcedureCodes.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ProcedureCodeId, ct);
        if (code is null) return Result<TreatmentPlanItem>.Failure("Procedure code not found.");

        if (code.RequiresTooth && request.ToothId is null)
            return Result<TreatmentPlanItem>.Failure($"{code.Code} requires a tooth to be selected.");
        if (code.RequiresSurfaces && request.Surfaces == ToothSurface.None)
            return Result<TreatmentPlanItem>.Failure($"{code.Code} requires at least one surface.");
        if (code.RequiresQuadrant && request.Quadrant == Quadrant.None)
            return Result<TreatmentPlanItem>.Failure($"{code.Code} requires a quadrant.");
        if (code.RequiresArch && request.Arch is null)
            return Result<TreatmentPlanItem>.Failure($"{code.Code} requires an arch.");

        var schedule = phase.TreatmentPlan.FeeScheduleId is { } scheduleId
            ? await db.FeeSchedules.AsNoTracking().Include(f => f.Items)
                .FirstOrDefaultAsync(f => f.Id == scheduleId, ct)
            : null;

        var unitFee = request.FeeOverride ?? _fees.ResolveFee(code, schedule);

        var item = new TreatmentPlanItem
        {
            TreatmentPlanPhaseId = phaseId,
            ProcedureCodeId = code.Id,
            ToothId = request.ToothId,
            Surfaces = request.Surfaces,
            Quadrant = request.Quadrant,
            Arch = request.Arch,
            Quantity = Math.Max(1, request.Quantity),
            UnitFee = unitFee,
            DiscountAmount = request.DiscountAmount,
            ProviderId = request.ProviderId ?? phase.TreatmentPlan.ProviderId,
            Priority = request.Priority,
            Notes = request.Notes,
            Sequence = phase.Items.Count + 1,
            PreAuthorisationRequired = unitFee >= 500m
        };

        await ApplyInsuranceEstimateAsync(phase.TreatmentPlan.PatientId, code, item, ct);

        db.TreatmentPlanItems.Add(item);
        await db.SaveChangesAsync(ct);
        await RecalculateAsync(phase.TreatmentPlanId, ct);

        return Result<TreatmentPlanItem>.Success(item);
    }

    public async Task<Result> RemoveItemAsync(Guid itemId, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.TreatmentPlansEdit, ct);
        var item = await db.TreatmentPlanItems
            .Include(i => i.TreatmentPlanPhase)
            .FirstOrDefaultAsync(i => i.Id == itemId, ct);

        if (item is null) return Result.Failure("Plan item not found.");
        if (item.Status is TreatmentPlanItemStatus.Completed or TreatmentPlanItemStatus.InProgress)
            return Result.Failure("Work already started on this item; mark it deferred instead of removing it.");

        var planId = item.TreatmentPlanPhase!.TreatmentPlanId;
        db.TreatmentPlanItems.Remove(item);
        await db.SaveChangesAsync(ct);
        await RecalculateAsync(planId, ct);
        return Result.Success();
    }

    public async Task<Result> SetItemStatusAsync(
        Guid itemId, TreatmentPlanItemStatus status, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.TreatmentPlansEdit, ct);
        var item = await db.TreatmentPlanItems
            .Include(i => i.TreatmentPlanPhase)
            .FirstOrDefaultAsync(i => i.Id == itemId, ct);

        if (item is null) return Result.Failure("Plan item not found.");

        item.Status = status;
        if (status == TreatmentPlanItemStatus.Completed) item.CompletedOn = clock.Today;

        await db.SaveChangesAsync(ct);
        await RecalculateAsync(item.TreatmentPlanPhase!.TreatmentPlanId, ct);
        return Result.Success();
    }

    /// <summary>Marks the plan as presented and starts the acceptance clock.</summary>
    public async Task<Result> PresentAsync(Guid planId, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.TreatmentPlansEdit, ct);
        var plan = await db.TreatmentPlans.Include(p => p.Phases).ThenInclude(ph => ph.Items)
            .FirstOrDefaultAsync(p => p.Id == planId, ct);

        if (plan is null) return Result.Failure("Plan not found.");
        if (!plan.AllItems.Any()) return Result.Failure("Add at least one procedure before presenting the plan.");

        plan.Status = TreatmentPlanStatus.Presented;
        plan.PresentedOn = clock.Today;
        plan.PresentedBy = currentUser.DisplayName ?? currentUser.UserName;
        plan.ValidUntil ??= clock.Today.AddDays(90);

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <summary>Records the patient's decision on each item and rolls it up to the plan.</summary>
    public async Task<Result> RecordDecisionAsync(
        Guid planId, IReadOnlyDictionary<Guid, bool> itemAcceptance,
        bool consentObtained, string? declineReason, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.TreatmentPlansApprove, ct);
        var plan = await db.TreatmentPlans
            .Include(p => p.Phases).ThenInclude(ph => ph.Items)
            .FirstOrDefaultAsync(p => p.Id == planId, ct);

        if (plan is null) return Result.Failure("Plan not found.");

        foreach (var item in plan.AllItems)
        {
            if (!itemAcceptance.TryGetValue(item.Id, out var accepted)) continue;
            if (item.Status is TreatmentPlanItemStatus.Completed or TreatmentPlanItemStatus.InProgress) continue;

            item.Status = accepted ? TreatmentPlanItemStatus.Accepted : TreatmentPlanItemStatus.Declined;
        }

        var total = plan.AllItems.Count();
        var acceptedCount = plan.AllItems.Count(i => i.Status == TreatmentPlanItemStatus.Accepted);

        plan.Status = acceptedCount switch
        {
            0 => TreatmentPlanStatus.Declined,
            _ when acceptedCount == total => TreatmentPlanStatus.Accepted,
            _ => TreatmentPlanStatus.PartiallyAccepted
        };

        plan.DecisionOn = clock.Today;
        plan.DeclineReason = acceptedCount == 0 ? declineReason : null;
        plan.ConsentObtained = consentObtained;
        if (consentObtained) plan.ConsentSignedAtUtc = clock.UtcNow;

        await db.SaveChangesAsync(ct);
        await RecalculateAsync(planId, ct);

        logger.LogInformation("Plan {Number}: {Accepted} of {Total} items accepted.",
            plan.PlanNumber, acceptedCount, total);

        return Result.Success();
    }

    /// <summary>Recomputes the plan totals from its items.</summary>
    /// <summary>
    /// Re-totals a plan after its items change. Internal, and called only from
    /// the plan operations that have already demanded their own permission.
    /// </summary>
    public async Task RecalculateAsync(Guid planId, CancellationToken ct = default)
    {
        var plan = await db.TreatmentPlans
            .Include(p => p.Phases).ThenInclude(ph => ph.Items)
            .FirstOrDefaultAsync(p => p.Id == planId, ct);

        if (plan is null) return;

        var live = plan.AllItems.Where(i => i.Status != TreatmentPlanItemStatus.Removed).ToList();

        plan.TotalFee = live.Sum(i => i.GrossFee);
        plan.TotalDiscount = live.Sum(i => i.DiscountAmount);
        plan.EstimatedInsurancePortion = live.Sum(i => i.EstimatedInsurance);
        plan.EstimatedPatientPortion = live.Sum(i => i.NetFee) - plan.EstimatedInsurancePortion;

        if (live.Count > 0 && live.All(i => i.Status == TreatmentPlanItemStatus.Completed))
        {
            plan.Status = TreatmentPlanStatus.Completed;
            plan.CompletedOn ??= clock.Today;
        }
        else if (live.Any(i => i.Status is TreatmentPlanItemStatus.InProgress or TreatmentPlanItemStatus.Completed)
                 && plan.Status is TreatmentPlanStatus.Accepted or TreatmentPlanStatus.PartiallyAccepted)
        {
            plan.Status = TreatmentPlanStatus.InProgress;
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task ApplyInsuranceEstimateAsync(
        Guid patientId, ProcedureCode code, TreatmentPlanItem item, CancellationToken ct)
    {
        var policies = await db.PatientInsurances.AsNoTracking()
            .Include(p => p.InsurancePlan)
            .Where(p => p.PatientId == patientId && p.IsActive)
            .ToListAsync(ct);

        var estimate = _estimator.EstimateCoordinated(code, item.NetFee, policies);
        item.EstimatedInsurance = estimate.InsurancePortion;
        item.EstimatedPatient = estimate.PatientPortion;
    }

    /// <summary>Creates a new version of a plan, leaving the original for the record.</summary>
    public async Task<Result<TreatmentPlan>> CreateRevisionAsync(Guid planId, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.TreatmentPlansEdit, ct);
        var original = await GetAsync(planId, ct);
        if (original is null) return Result<TreatmentPlan>.Failure("Plan not found.");

        var revision = new TreatmentPlan
        {
            PatientId = original.PatientId,
            PlanNumber = await sequences.NextAsync(SequenceNames.TreatmentPlan, ct),
            Name = original.Name,
            Description = original.Description,
            VersionNumber = original.VersionNumber + 1,
            SupersedesPlanId = original.Id,
            ProviderId = original.ProviderId,
            FeeScheduleId = original.FeeScheduleId,
            CreatedOn = clock.Today,
            ValidUntil = clock.Today.AddDays(90),
            Status = TreatmentPlanStatus.Draft
        };

        foreach (var phase in original.Phases.OrderBy(p => p.PhaseNumber))
        {
            var copy = new TreatmentPlanPhase
            {
                TreatmentPlanId = revision.Id,
                PhaseNumber = phase.PhaseNumber,
                Name = phase.Name,
                Description = phase.Description,
                ClinicalObjective = phase.ClinicalObjective,
                Priority = phase.Priority
            };

            // Completed work stays on the original plan; only outstanding items carry over.
            foreach (var item in phase.Items.Where(i => i.Status != TreatmentPlanItemStatus.Completed))
            {
                copy.Items.Add(new TreatmentPlanItem
                {
                    TreatmentPlanPhaseId = copy.Id,
                    ProcedureCodeId = item.ProcedureCodeId,
                    ToothId = item.ToothId,
                    Surfaces = item.Surfaces,
                    Quadrant = item.Quadrant,
                    Arch = item.Arch,
                    ProviderId = item.ProviderId,
                    Quantity = item.Quantity,
                    UnitFee = item.UnitFee,
                    DiscountAmount = item.DiscountAmount,
                    EstimatedInsurance = item.EstimatedInsurance,
                    EstimatedPatient = item.EstimatedPatient,
                    Priority = item.Priority,
                    Sequence = item.Sequence,
                    Notes = item.Notes
                });
            }

            revision.Phases.Add(copy);
        }

        var tracked = await db.TreatmentPlans.FirstAsync(p => p.Id == planId, ct);
        tracked.Status = TreatmentPlanStatus.Superseded;

        db.TreatmentPlans.Add(revision);
        await db.SaveChangesAsync(ct);
        await RecalculateAsync(revision.Id, ct);

        return Result<TreatmentPlan>.Success(revision);
    }
}
