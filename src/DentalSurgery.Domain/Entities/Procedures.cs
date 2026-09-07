using DentalSurgery.Domain.Common;
using DentalSurgery.Domain.Enums;

namespace DentalSurgery.Domain.Entities;

/// <summary>Billable procedure catalogue entry (CDT/OPCS style code plus defaults).</summary>
public class ProcedureCode : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string? LongDescription { get; set; }
    public ProcedureCategory Category { get; set; } = ProcedureCategory.Diagnostic;
    public string? SubCategory { get; set; }

    public decimal DefaultFee { get; set; }
    public int DefaultDurationMinutes { get; set; } = 30;

    public bool RequiresTooth { get; set; }
    public bool RequiresSurfaces { get; set; }
    public bool RequiresQuadrant { get; set; }
    public bool RequiresArch { get; set; }
    public bool RequiresRootCount { get; set; }

    public bool IsSurgical { get; set; }
    public bool RequiresAnaesthesia { get; set; }
    public bool RequiresLabWork { get; set; }
    public bool RequiresConsent { get; set; }
    public bool RequiresRadiograph { get; set; }
    public bool IsDiagnosticOnly { get; set; }
    public bool IsHygieneProcedure { get; set; }
    public bool GeneratesRecall { get; set; }
    public int? RecallIntervalMonths { get; set; }

    /// <summary>Number of visits the procedure normally takes.</summary>
    public int TypicalVisits { get; set; } = 1;

    public string? InsuranceCategory { get; set; }
    public decimal? TypicalInsuranceCoveragePercent { get; set; }
    public bool IsTaxable { get; set; }
    public string? ClinicalNoteTemplate { get; set; }
    public string? PostOperativeInstructionCode { get; set; }
    public string? ColourHex { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public ICollection<FeeScheduleItem> FeeScheduleItems { get; set; } = new List<FeeScheduleItem>();

    public string Display => $"{Code} - {ShortDescription}";
}

/// <summary>A named price list. Practice, insurer-specific or discount.</summary>
public class FeeSchedule : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public FeeScheduleType ScheduleType { get; set; } = FeeScheduleType.Practice;
    public Guid? InsuranceCarrierId { get; set; }
    public InsuranceCarrier? InsuranceCarrier { get; set; }

    public DateOnly EffectiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? EffectiveTo { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Applied to catalogue fees when no explicit item row exists.</summary>
    public decimal? BlanketAdjustmentPercent { get; set; }

    public ICollection<FeeScheduleItem> Items { get; set; } = new List<FeeScheduleItem>();
}

public class FeeScheduleItem : BaseEntity
{
    public Guid FeeScheduleId { get; set; }
    public FeeSchedule? FeeSchedule { get; set; }

    public Guid ProcedureCodeId { get; set; }
    public ProcedureCode? ProcedureCode { get; set; }

    public decimal Fee { get; set; }
    public decimal? AllowedAmount { get; set; }
    public decimal? CoveragePercent { get; set; }
    public string? Notes { get; set; }
}

/// <summary>A proposed course of treatment, organised into phases.</summary>
public class TreatmentPlan : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public string PlanNumber { get; set; } = string.Empty;
    public string Name { get; set; } = "Treatment Plan";
    public string? Description { get; set; }
    public int VersionNumber { get; set; } = 1;
    public Guid? SupersedesPlanId { get; set; }

    public Guid? ProviderId { get; set; }
    public Staff? Provider { get; set; }

    public TreatmentPlanStatus Status { get; set; } = TreatmentPlanStatus.Draft;
    public DateOnly CreatedOn { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? PresentedOn { get; set; }
    public DateOnly? DecisionOn { get; set; }
    public DateOnly? ValidUntil { get; set; }
    public DateOnly? CompletedOn { get; set; }

    public Guid? FeeScheduleId { get; set; }
    public FeeSchedule? FeeSchedule { get; set; }

    public decimal TotalFee { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal EstimatedInsurancePortion { get; set; }
    public decimal EstimatedPatientPortion { get; set; }

    public bool ConsentObtained { get; set; }
    public DateTime? ConsentSignedAtUtc { get; set; }
    public string? ConsentSignatureData { get; set; }
    public string? PresentedBy { get; set; }
    public string? DeclineReason { get; set; }
    public string? Notes { get; set; }
    public string? RisksDiscussed { get; set; }
    public string? AlternativesDiscussed { get; set; }

    public ICollection<TreatmentPlanPhase> Phases { get; set; } = new List<TreatmentPlanPhase>();

    public IEnumerable<TreatmentPlanItem> AllItems => Phases.SelectMany(p => p.Items);

    public decimal AcceptedValue =>
        AllItems.Where(i => i.Status is TreatmentPlanItemStatus.Accepted
                              or TreatmentPlanItemStatus.Scheduled
                              or TreatmentPlanItemStatus.InProgress
                              or TreatmentPlanItemStatus.Completed)
                .Sum(i => i.NetFee);

    public decimal AcceptanceRatePercent =>
        TotalFee <= 0 ? 0 : Math.Round(100m * AcceptedValue / TotalFee, 1);

    public int CompletedItemCount => AllItems.Count(i => i.Status == TreatmentPlanItemStatus.Completed);
    public int TotalItemCount => AllItems.Count();
}

/// <summary>A sequenced stage within a plan, e.g. "Phase 1 - Stabilisation".</summary>
public class TreatmentPlanPhase : BaseEntity
{
    public Guid TreatmentPlanId { get; set; }
    public TreatmentPlan? TreatmentPlan { get; set; }

    public int PhaseNumber { get; set; } = 1;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ClinicalObjective { get; set; }
    public TreatmentPriority Priority { get; set; } = TreatmentPriority.Routine;
    public int EstimatedVisits { get; set; } = 1;
    public int? EstimatedWeeksToComplete { get; set; }

    public ICollection<TreatmentPlanItem> Items { get; set; } = new List<TreatmentPlanItem>();

    public decimal PhaseTotal => Items.Sum(i => i.NetFee);
}

/// <summary>One proposed procedure inside a plan phase.</summary>
public class TreatmentPlanItem : BaseEntity
{
    public Guid TreatmentPlanPhaseId { get; set; }
    public TreatmentPlanPhase? TreatmentPlanPhase { get; set; }

    public Guid ProcedureCodeId { get; set; }
    public ProcedureCode? ProcedureCode { get; set; }

    public Guid? ToothId { get; set; }
    public Tooth? Tooth { get; set; }
    public ToothSurface Surfaces { get; set; } = ToothSurface.None;
    public Quadrant Quadrant { get; set; } = Quadrant.None;
    public DentalArch? Arch { get; set; }

    public Guid? ProviderId { get; set; }
    public Staff? Provider { get; set; }

    public int Sequence { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal EstimatedInsurance { get; set; }
    public decimal EstimatedPatient { get; set; }

    public TreatmentPlanItemStatus Status { get; set; } = TreatmentPlanItemStatus.Proposed;
    public TreatmentPriority Priority { get; set; } = TreatmentPriority.Routine;
    public DateOnly? ScheduledFor { get; set; }
    public DateOnly? CompletedOn { get; set; }
    public Guid? CompletedProcedureId { get; set; }
    public bool PreAuthorisationRequired { get; set; }
    public bool PreAuthorisationObtained { get; set; }
    public string? Notes { get; set; }

    public decimal GrossFee => UnitFee * Quantity;
    public decimal NetFee => GrossFee - DiscountAmount;
    public string Description =>
        ProcedureCode is null
            ? "Procedure"
            : Tooth is null
                ? ProcedureCode.ShortDescription
                : $"{ProcedureCode.ShortDescription} - tooth {Tooth.FdiNumber}{(Surfaces == ToothSurface.None ? "" : " " + SurfaceNotation.ToCode(Surfaces))}";
}

/// <summary>A procedure that has actually been carried out (or is in progress).</summary>
public class Procedure : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid ProcedureCodeId { get; set; }
    public ProcedureCode? ProcedureCode { get; set; }

    public Guid? AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public Guid? TreatmentPlanItemId { get; set; }
    public TreatmentPlanItem? TreatmentPlanItem { get; set; }

    public Guid? ToothId { get; set; }
    public Tooth? Tooth { get; set; }
    public ToothSurface Surfaces { get; set; } = ToothSurface.None;
    public Quadrant Quadrant { get; set; } = Quadrant.None;
    public DentalArch? Arch { get; set; }
    public int? RootCanalCount { get; set; }

    public Guid? ProviderId { get; set; }
    public Staff? Provider { get; set; }
    public Guid? AssistantId { get; set; }
    public Staff? Assistant { get; set; }
    public Guid? LocationId { get; set; }
    public Location? Location { get; set; }

    public DateTime DateOfService { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public ProcedureStatus Status { get; set; } = ProcedureStatus.Planned;
    public int Quantity { get; set; } = 1;
    public decimal Fee { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public bool IsBillable { get; set; } = true;
    public bool IsWarrantyRedo { get; set; }
    public Guid? RedoOfProcedureId { get; set; }

    public RestorationMaterial Material { get; set; } = RestorationMaterial.None;
    public string? ShadeReference { get; set; }
    public string? BatchNumber { get; set; }
    public string? Notes { get; set; }
    public string? Complications { get; set; }

    public Guid? InvoiceLineId { get; set; }
    public InvoiceLine? InvoiceLine { get; set; }

    public SurgicalRecord? SurgicalRecord { get; set; }
    public AnaesthesiaRecord? AnaesthesiaRecord { get; set; }
    public ICollection<ProcedureMaterialUsage> MaterialsUsed { get; set; } = new List<ProcedureMaterialUsage>();

    public decimal NetFee => (Fee * Quantity) - DiscountAmount + TaxAmount;

    public int? DurationMinutes =>
        StartedAtUtc.HasValue && CompletedAtUtc.HasValue
            ? (int)(CompletedAtUtc.Value - StartedAtUtc.Value).TotalMinutes
            : null;

    public string Description
    {
        get
        {
            var baseText = ProcedureCode?.ShortDescription ?? "Procedure";
            if (Tooth is null) return baseText;
            var surfaces = Surfaces == ToothSurface.None ? string.Empty : $" {SurfaceNotation.ToCode(Surfaces)}";
            return $"{baseText} - tooth {Tooth.FdiNumber}{surfaces}";
        }
    }
}

/// <summary>Stock consumed by a procedure, used for costing and inventory depletion.</summary>
public class ProcedureMaterialUsage : BaseEntity
{
    public Guid ProcedureId { get; set; }
    public Procedure? Procedure { get; set; }

    public Guid InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }

    public Guid? InventoryLotId { get; set; }
    public InventoryLot? InventoryLot { get; set; }

    public decimal Quantity { get; set; } = 1;
    public decimal? UnitCost { get; set; }
    public string? Notes { get; set; }

    public decimal TotalCost => Quantity * (UnitCost ?? 0m);
}

/// <summary>Operative detail for a surgical procedure.</summary>
public class SurgicalRecord : BaseEntity
{
    public Guid ProcedureId { get; set; }
    public Procedure? Procedure { get; set; }

    public SurgeryType SurgeryType { get; set; } = SurgeryType.SimpleExtraction;
    public AsaClassification AsaClassification { get; set; } = AsaClassification.AsaI;

    public DateTime? IncisionTimeUtc { get; set; }
    public DateTime? ClosureTimeUtc { get; set; }
    public int? DurationMinutes { get; set; }

    public bool FlapRaised { get; set; }
    public string? FlapDesign { get; set; }
    public bool BoneRemoval { get; set; }
    public bool ToothSectioned { get; set; }
    public bool SutureRequired { get; set; }
    public string? SutureMaterial { get; set; }
    public int? SutureCount { get; set; }
    public DateOnly? SutureRemovalDue { get; set; }

    public bool GraftPlaced { get; set; }
    public string? GraftMaterial { get; set; }
    public string? GraftVolume { get; set; }
    public bool MembranePlaced { get; set; }
    public string? MembraneType { get; set; }

    public bool SpecimenSentForHistology { get; set; }
    public string? SpecimenReference { get; set; }
    public string? HistologyResult { get; set; }

    public string? EstimatedBloodLoss { get; set; }
    public bool HaemostasisAchieved { get; set; } = true;
    public string? Irrigation { get; set; }
    public string? Instrumentation { get; set; }
    public Guid? InstrumentSetId { get; set; }
    public InstrumentSet? InstrumentSet { get; set; }

    public SurgicalOutcome Outcome { get; set; } = SurgicalOutcome.Uneventful;
    public string? Complications { get; set; }
    public bool NerveProximityWarningGiven { get; set; }
    public bool SinusExposure { get; set; }
    public bool PostOperativeInstructionsGiven { get; set; } = true;
    public string? PostOperativeMedication { get; set; }
    public DateOnly? FollowUpDue { get; set; }

    public string? OperativeNote { get; set; }
    public string? Findings { get; set; }
}

/// <summary>Anaesthesia given for a procedure, including a sedation monitoring record.</summary>
public class AnaesthesiaRecord : BaseEntity
{
    public Guid ProcedureId { get; set; }
    public Procedure? Procedure { get; set; }

    public AnaesthesiaType AnaesthesiaType { get; set; } = AnaesthesiaType.LocalInfiltration;
    public Guid? AdministeredByStaffId { get; set; }
    public Staff? AdministeredByStaff { get; set; }

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAtUtc { get; set; }
    public DateTime? RecoveryCompleteAtUtc { get; set; }

    public bool ConsentObtained { get; set; } = true;
    public bool PreOperativeAssessmentDone { get; set; } = true;
    public bool FastingConfirmed { get; set; }
    public bool EscortConfirmed { get; set; }
    public AsaClassification AsaClassification { get; set; } = AsaClassification.AsaI;

    public bool TopicalApplied { get; set; }
    public string? TopicalAgent { get; set; }

    public int? BaselineSystolicBp { get; set; }
    public int? BaselineDiastolicBp { get; set; }
    public int? BaselinePulse { get; set; }
    public int? BaselineOxygenSaturation { get; set; }
    public int? LowestOxygenSaturation { get; set; }

    public bool MonitoringPulseOximetry { get; set; }
    public bool MonitoringBloodPressure { get; set; }
    public bool MonitoringCapnography { get; set; }
    public bool MonitoringEcg { get; set; }

    public bool ReversalAgentGiven { get; set; }
    public string? ReversalAgent { get; set; }
    public bool DischargeCriteriaMet { get; set; }
    public string? AdverseEvents { get; set; }
    public string? Notes { get; set; }

    public ICollection<AnaesthesiaAgentDose> Doses { get; set; } = new List<AnaesthesiaAgentDose>();

    public decimal TotalMilligrams => Doses.Sum(d => d.TotalMilligrams);
}

/// <summary>One cartridge/dose of an anaesthetic agent.</summary>
public class AnaesthesiaAgentDose : BaseEntity
{
    public Guid AnaesthesiaRecordId { get; set; }
    public AnaesthesiaRecord? AnaesthesiaRecord { get; set; }

    public string AgentName { get; set; } = string.Empty;
    public string? Concentration { get; set; }
    public string? Vasoconstrictor { get; set; }
    public decimal Cartridges { get; set; } = 1;
    public decimal MillilitresPerCartridge { get; set; } = 2.2m;
    public decimal MilligramsPerMillilitre { get; set; } = 20m;

    public InjectionTechnique Technique { get; set; } = InjectionTechnique.Infiltration;
    public string? Site { get; set; }
    public Guid? ToothId { get; set; }
    public DateTime AdministeredAtUtc { get; set; } = DateTime.UtcNow;
    public bool AspirationNegative { get; set; } = true;
    public string? BatchNumber { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string? Notes { get; set; }

    public decimal TotalMillilitres => Cartridges * MillilitresPerCartridge;
    public decimal TotalMilligrams => TotalMillilitres * MilligramsPerMillilitre;
}

/// <summary>Registry entry for a placed dental implant, tracked for its lifetime.</summary>
public class DentalImplant : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid ToothId { get; set; }
    public Tooth? Tooth { get; set; }

    public Guid? PlacementProcedureId { get; set; }
    public Procedure? PlacementProcedure { get; set; }

    public string Manufacturer { get; set; } = string.Empty;
    public string SystemName { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public string? LotNumber { get; set; }
    public string? SerialNumber { get; set; }

    public decimal DiameterMm { get; set; }
    public decimal LengthMm { get; set; }
    public string? Platform { get; set; }
    public string? SurfaceTreatment { get; set; }
    public string? ConnectionType { get; set; }

    public DateOnly PlacementDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public Guid? SurgeonStaffId { get; set; }
    public Staff? SurgeonStaff { get; set; }

    public int? InsertionTorqueNcm { get; set; }
    public int? StabilityQuotientIsq { get; set; }
    public BoneQuality BoneQuality { get; set; } = BoneQuality.Unknown;
    public bool GuidedSurgery { get; set; }
    public bool ImmediatePlacement { get; set; }
    public bool ImmediateLoading { get; set; }
    public bool GraftUsed { get; set; }
    public string? GraftDetail { get; set; }
    public bool MembraneUsed { get; set; }

    public DateOnly? HealingAbutmentDate { get; set; }
    public DateOnly? SecondStageDate { get; set; }
    public DateOnly? ImpressionDate { get; set; }
    public DateOnly? RestorationDate { get; set; }
    public string? AbutmentType { get; set; }
    public string? RestorationType { get; set; }

    public ImplantStatus Status { get; set; } = ImplantStatus.Placed;
    public DateOnly? FailureDate { get; set; }
    public string? FailureReason { get; set; }
    public int? WarrantyYears { get; set; }
    public DateOnly? NextReviewDue { get; set; }
    public string? Notes { get; set; }

    public string Display => $"{Manufacturer} {SystemName} {DiameterMm}x{LengthMm}mm";
}

/// <summary>Reusable aftercare text issued to patients.</summary>
public class PostOperativeInstruction : LookupEntity
{
    public string Body { get; set; } = string.Empty;
    public string? AppliesToProcedureCodes { get; set; }
    public SurgeryType? AppliesToSurgeryType { get; set; }
    public string? WarningSigns { get; set; }
    public string? EmergencyContactText { get; set; }
}
