using DentalSurgery.Domain.Common;
using DentalSurgery.Domain.Enums;

namespace DentalSurgery.Domain.Entities;

// ------------------------------------------------------------------ Insurance

public class InsuranceCarrier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? PayerId { get; set; }
    public string? ElectronicPayerId { get; set; }
    public Address Address { get; set; } = new();
    public Address? ClaimsAddress { get; set; }
    public ContactDetails Contact { get; set; } = new();
    public string? ClaimsPhone { get; set; }
    public string? Website { get; set; }
    public bool AcceptsElectronicClaims { get; set; } = true;
    public int TypicalPaymentDays { get; set; } = 30;
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    public ICollection<InsurancePlan> Plans { get; set; } = new List<InsurancePlan>();
}

public class InsurancePlan : BaseEntity
{
    public Guid InsuranceCarrierId { get; set; }
    public InsuranceCarrier? InsuranceCarrier { get; set; }

    public string PlanName { get; set; } = string.Empty;
    public string? GroupNumber { get; set; }
    public string? GroupName { get; set; }
    public InsurancePlanType PlanType { get; set; } = InsurancePlanType.Ppo;

    public decimal? AnnualMaximum { get; set; }
    public decimal? LifetimeMaximum { get; set; }
    public decimal? IndividualDeductible { get; set; }
    public decimal? FamilyDeductible { get; set; }
    public bool DeductibleAppliesToPreventive { get; set; }

    public decimal PreventiveCoveragePercent { get; set; } = 100m;
    public decimal DiagnosticCoveragePercent { get; set; } = 100m;
    public decimal BasicCoveragePercent { get; set; } = 80m;
    public decimal MajorCoveragePercent { get; set; } = 50m;
    public decimal EndodonticCoveragePercent { get; set; } = 80m;
    public decimal PeriodonticCoveragePercent { get; set; } = 80m;
    public decimal OrthodonticCoveragePercent { get; set; } = 50m;
    public decimal? OrthodonticLifetimeMaximum { get; set; }
    public decimal ImplantCoveragePercent { get; set; }
    public decimal SurgeryCoveragePercent { get; set; } = 50m;

    public int? WaitingPeriodBasicMonths { get; set; }
    public int? WaitingPeriodMajorMonths { get; set; }
    public int? WaitingPeriodOrthoMonths { get; set; }

    public string? BenefitYearStartMonth { get; set; } = "January";
    public bool RequiresPreAuthorisation { get; set; }
    public decimal? PreAuthorisationThreshold { get; set; }
    public Guid? FeeScheduleId { get; set; }
    public FeeSchedule? FeeSchedule { get; set; }

    public bool IsActive { get; set; } = true;
    public string? CoverageNotes { get; set; }
    public string? Exclusions { get; set; }

    /// <summary>Coverage percentage for a procedure category under this plan.</summary>
    public decimal CoverageFor(ProcedureCategory category) => category switch
    {
        ProcedureCategory.Diagnostic or ProcedureCategory.Radiology => DiagnosticCoveragePercent,
        ProcedureCategory.Preventive => PreventiveCoveragePercent,
        ProcedureCategory.Restorative => BasicCoveragePercent,
        ProcedureCategory.Endodontics => EndodonticCoveragePercent,
        ProcedureCategory.Periodontics => PeriodonticCoveragePercent,
        ProcedureCategory.Orthodontics => OrthodonticCoveragePercent,
        ProcedureCategory.ImplantServices => ImplantCoveragePercent,
        ProcedureCategory.OralAndMaxillofacialSurgery => SurgeryCoveragePercent,
        ProcedureCategory.ProsthodonticsFixed or ProcedureCategory.ProsthodonticsRemovable
            or ProcedureCategory.MaxillofacialProsthetics => MajorCoveragePercent,
        _ => BasicCoveragePercent
    };
}

/// <summary>A patient's enrolment in an insurance plan.</summary>
public class PatientInsurance : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid InsurancePlanId { get; set; }
    public InsurancePlan? InsurancePlan { get; set; }

    public InsurancePriority Priority { get; set; } = InsurancePriority.Primary;
    public string? MemberId { get; set; }
    public string? PolicyNumber { get; set; }

    public bool SubscriberIsPatient { get; set; } = true;
    public PersonName? SubscriberName { get; set; }
    public DateOnly? SubscriberDateOfBirth { get; set; }
    public string? SubscriberId { get; set; }
    public ContactRelationship RelationshipToSubscriber { get; set; } = ContactRelationship.Unknown;
    public string? SubscriberEmployer { get; set; }

    public DateOnly EffectiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? TerminatedOn { get; set; }
    public bool IsActive { get; set; } = true;

    public decimal BenefitsUsedThisYear { get; set; }
    public decimal DeductibleMetThisYear { get; set; }
    public decimal? RemainingAnnualMaximum { get; set; }
    public DateOnly? BenefitsVerifiedOn { get; set; }
    public string? VerifiedBy { get; set; }
    public string? Notes { get; set; }

    public ICollection<InsuranceClaim> Claims { get; set; } = new List<InsuranceClaim>();

    public bool IsCurrentlyValid =>
        IsActive &&
        EffectiveFrom <= DateOnly.FromDateTime(DateTime.Today) &&
        (TerminatedOn is null || TerminatedOn >= DateOnly.FromDateTime(DateTime.Today));
}

// ------------------------------------------------------------------ Invoicing

public class Invoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;

    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    /// <summary>The person financially responsible, if not the patient.</summary>
    public Guid? GuarantorPatientId { get; set; }
    public Patient? GuarantorPatient { get; set; }

    public Guid? LocationId { get; set; }
    public Location? Location { get; set; }
    public Guid? ProviderId { get; set; }
    public Staff? Provider { get; set; }

    public DateOnly IssueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly DueDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(30));
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public decimal InsuranceEstimate { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal WriteOffAmount { get; set; }

    public DateOnly? PaidInFullOn { get; set; }
    public DateTime? VoidedAtUtc { get; set; }
    public string? VoidReason { get; set; }
    public string? PurchaseOrderReference { get; set; }
    public string? Notes { get; set; }
    public string? TermsText { get; set; }
    public int RemindersSent { get; set; }
    public DateOnly? LastReminderOn { get; set; }

    public ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
    public ICollection<PaymentAllocation> PaymentAllocations { get; set; } = new List<PaymentAllocation>();

    public decimal Balance => Total - AmountPaid - WriteOffAmount;
    public bool IsOverdue => Balance > 0 && DueDate < DateOnly.FromDateTime(DateTime.Today);
    public int DaysOverdue =>
        IsOverdue ? DateOnly.FromDateTime(DateTime.Today).DayNumber - DueDate.DayNumber : 0;
}

public class InvoiceLine : BaseEntity
{
    public Guid InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    public Guid? ProcedureId { get; set; }
    public Guid? ProcedureCodeId { get; set; }
    public ProcedureCode? ProcedureCode { get; set; }
    public Guid? ToothId { get; set; }
    public Tooth? Tooth { get; set; }

    public int Sequence { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateOnly ServiceDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRatePercent { get; set; }
    public decimal InsurancePortion { get; set; }
    public decimal PatientPortion { get; set; }
    public Guid? ProviderId { get; set; }

    public decimal Gross => Quantity * UnitPrice;
    public decimal NetBeforeTax => Gross - DiscountAmount;
    public decimal TaxAmount => Math.Round(NetBeforeTax * TaxRatePercent / 100m, 2);
    public decimal LineTotal => NetBeforeTax + TaxAmount;
}

public class Payment : BaseEntity
{
    public string PaymentNumber { get; set; } = string.Empty;

    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public DateOnly PaymentDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; } = PaymentMethod.CreditCard;
    public PaymentStatus Status { get; set; } = PaymentStatus.Cleared;

    public string? ReferenceNumber { get; set; }
    public string? CardLastFour { get; set; }
    public string? CardType { get; set; }
    public string? AuthorisationCode { get; set; }
    public string? BankReference { get; set; }
    public string? ChequeNumber { get; set; }

    public Guid? InsuranceClaimId { get; set; }
    public InsuranceClaim? InsuranceClaim { get; set; }
    public Guid? PaymentPlanInstallmentId { get; set; }
    public Guid? LocationId { get; set; }

    public decimal RefundedAmount { get; set; }
    public DateOnly? RefundedOn { get; set; }
    public string? RefundReason { get; set; }

    public string? ReceivedBy { get; set; }
    public string? Notes { get; set; }

    public ICollection<PaymentAllocation> Allocations { get; set; } = new List<PaymentAllocation>();

    public decimal AllocatedAmount => Allocations.Sum(a => a.Amount);
    public decimal UnallocatedAmount => Amount - RefundedAmount - AllocatedAmount;
    public bool IsFullyAllocated => UnallocatedAmount <= 0.005m;
}

/// <summary>Applies part of a payment to a specific invoice.</summary>
public class PaymentAllocation : BaseEntity
{
    public Guid PaymentId { get; set; }
    public Payment? Payment { get; set; }

    public Guid InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public Guid? InvoiceLineId { get; set; }

    public decimal Amount { get; set; }
    public DateOnly AllocatedOn { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string? Notes { get; set; }
}

/// <summary>Append-only account history for a patient.</summary>
public class LedgerEntry : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public DateOnly EntryDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public LedgerEntryType EntryType { get; set; }
    public string Description { get; set; } = string.Empty;

    /// <summary>Increases what the patient owes.</summary>
    public decimal Debit { get; set; }

    /// <summary>Reduces what the patient owes.</summary>
    public decimal Credit { get; set; }

    public decimal RunningBalance { get; set; }

    public Guid? InvoiceId { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? ProcedureId { get; set; }
    public Guid? AdjustmentId { get; set; }
    public Guid? InsuranceClaimId { get; set; }
    public Guid? ProviderId { get; set; }
    public string? Reference { get; set; }

    public decimal SignedAmount => Debit - Credit;
}

public class AccountAdjustment : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public DateOnly AdjustmentDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public AdjustmentType AdjustmentType { get; set; } = AdjustmentType.Discount;

    /// <summary>Positive reduces the balance; negative increases it.</summary>
    public decimal Amount { get; set; }

    public Guid? InvoiceId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string? Notes { get; set; }
}

public class PaymentPlan : BaseEntity
{
    public string PlanNumber { get; set; } = string.Empty;

    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }
    public Guid? TreatmentPlanId { get; set; }

    public decimal TotalAmount { get; set; }
    public decimal DownPayment { get; set; }
    public decimal InstallmentAmount { get; set; }
    public PaymentFrequency Frequency { get; set; } = PaymentFrequency.Monthly;
    public int NumberOfInstallments { get; set; }
    public decimal InterestRatePercent { get; set; }
    public decimal AdministrationFee { get; set; }

    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? EndDate { get; set; }
    public PaymentPlanStatus Status { get; set; } = PaymentPlanStatus.Draft;

    public bool AgreementSigned { get; set; }
    public DateTime? AgreementSignedAtUtc { get; set; }
    public string? SignatureData { get; set; }
    public string? Notes { get; set; }

    public ICollection<PaymentPlanInstallment> Installments { get; set; } = new List<PaymentPlanInstallment>();

    public decimal AmountPaid => Installments.Sum(i => i.AmountPaid) + DownPayment;
    public decimal AmountRemaining => TotalAmount - AmountPaid;
    public int InstallmentsPaid => Installments.Count(i => i.Status == InstallmentStatus.Paid);
    public bool IsInArrears => Installments.Any(i => i.Status == InstallmentStatus.Overdue);
}

public class PaymentPlanInstallment : BaseEntity
{
    public Guid PaymentPlanId { get; set; }
    public PaymentPlan? PaymentPlan { get; set; }

    public int InstallmentNumber { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public DateOnly? PaidOn { get; set; }
    public InstallmentStatus Status { get; set; } = InstallmentStatus.Scheduled;
    public Guid? PaymentId { get; set; }
    public int FailedAttempts { get; set; }
    public string? Notes { get; set; }

    public decimal Outstanding => AmountDue - AmountPaid;
}

// ------------------------------------------------------------------ Claims

public class InsuranceClaim : BaseEntity
{
    public string ClaimNumber { get; set; } = string.Empty;

    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid PatientInsuranceId { get; set; }
    public PatientInsurance? PatientInsurance { get; set; }

    public Guid? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public Guid? ProviderId { get; set; }
    public Staff? Provider { get; set; }

    public ClaimStatus Status { get; set; } = ClaimStatus.Draft;
    public bool IsPreAuthorisation { get; set; }
    public DateOnly ServiceDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? SubmittedOn { get; set; }
    public DateOnly? AcknowledgedOn { get; set; }
    public DateOnly? AdjudicatedOn { get; set; }
    public DateOnly? PaidOn { get; set; }

    public decimal TotalCharged { get; set; }
    public decimal TotalAllowed { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal DeductibleApplied { get; set; }
    public decimal PatientResponsibility { get; set; }
    public decimal WriteOffAmount { get; set; }

    public string? PayerClaimReference { get; set; }
    public string? SubmissionMethod { get; set; }
    public string? DenialReason { get; set; }
    public string? DenialCode { get; set; }
    public bool AppealSubmitted { get; set; }
    public DateOnly? AppealDate { get; set; }
    public string? AppealNotes { get; set; }

    public bool AttachmentsIncluded { get; set; }
    public bool NarrativeIncluded { get; set; }
    public string? Narrative { get; set; }
    public string? Notes { get; set; }
    public int ResubmissionCount { get; set; }

    public ICollection<InsuranceClaimLine> Lines { get; set; } = new List<InsuranceClaimLine>();

    public decimal Outstanding => TotalCharged - TotalPaid - WriteOffAmount - PatientResponsibility;
    public int? DaysOutstanding =>
        SubmittedOn is null || Status is ClaimStatus.Paid or ClaimStatus.Closed
            ? null
            : DateOnly.FromDateTime(DateTime.Today).DayNumber - SubmittedOn.Value.DayNumber;
}

public class InsuranceClaimLine : BaseEntity
{
    public Guid InsuranceClaimId { get; set; }
    public InsuranceClaim? InsuranceClaim { get; set; }

    public Guid? ProcedureId { get; set; }
    public Procedure? Procedure { get; set; }
    public Guid ProcedureCodeId { get; set; }
    public ProcedureCode? ProcedureCode { get; set; }
    public Guid? ToothId { get; set; }
    public Tooth? Tooth { get; set; }

    public int Sequence { get; set; }
    public string? SurfaceCode { get; set; }
    public DateOnly ServiceDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public decimal ChargedAmount { get; set; }
    public decimal AllowedAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DeductibleAmount { get; set; }
    public decimal CoInsuranceAmount { get; set; }
    public decimal WriteOffAmount { get; set; }
    public string? AdjudicationCode { get; set; }
    public string? DenialReason { get; set; }
    public string? Notes { get; set; }
}
