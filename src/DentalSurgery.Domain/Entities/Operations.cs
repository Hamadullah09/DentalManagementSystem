using DentalSurgery.Domain.Common;
using DentalSurgery.Domain.Enums;

namespace DentalSurgery.Domain.Entities;

// ------------------------------------------------------------------ Inventory

public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? AccountNumber { get; set; }
    public string? ContactPerson { get; set; }
    public ContactDetails Contact { get; set; } = new();
    public Address Address { get; set; } = new();
    public string? Website { get; set; }
    public string? PaymentTerms { get; set; }
    public int LeadTimeDays { get; set; } = 5;
    public decimal? MinimumOrderValue { get; set; }
    public bool IsPreferred { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    public ICollection<InventoryItem> Items { get; set; } = new List<InventoryItem>();
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}

public class InventoryItem : BaseEntity
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public InventoryCategory Category { get; set; } = InventoryCategory.Consumable;
    public string? SubCategory { get; set; }
    public string? Manufacturer { get; set; }
    public string? ManufacturerPartNumber { get; set; }
    public string? Barcode { get; set; }

    public string UnitOfMeasure { get; set; } = "each";
    public int UnitsPerPack { get; set; } = 1;

    public decimal CurrentStock { get; set; }
    public decimal ReorderLevel { get; set; }
    public decimal ReorderQuantity { get; set; }
    public decimal? MaximumStock { get; set; }

    public decimal UnitCost { get; set; }
    public decimal? LastPurchasePrice { get; set; }
    public DateOnly? LastPurchaseDate { get; set; }
    public decimal? SellingPrice { get; set; }

    public Guid? PreferredSupplierId { get; set; }
    public Supplier? PreferredSupplier { get; set; }
    public Guid? LocationId { get; set; }
    public string? StorageLocation { get; set; }

    public bool RequiresLotTracking { get; set; }
    public bool RequiresExpiryTracking { get; set; }
    public bool IsControlledSubstance { get; set; }
    public bool IsSterilisable { get; set; }
    public bool IsSingleUse { get; set; }
    public bool RequiresColdStorage { get; set; }
    public bool IsActive { get; set; } = true;
    public string? SafetyDataSheetPath { get; set; }
    public string? Notes { get; set; }

    public ICollection<InventoryLot> Lots { get; set; } = new List<InventoryLot>();
    public ICollection<StockMovement> Movements { get; set; } = new List<StockMovement>();

    public bool IsBelowReorderLevel => CurrentStock <= ReorderLevel;
    public bool IsOutOfStock => CurrentStock <= 0;
    public decimal StockValue => CurrentStock * UnitCost;
}

public class InventoryLot : BaseEntity
{
    public Guid InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }

    public string LotNumber { get; set; } = string.Empty;
    public DateOnly? ExpiryDate { get; set; }
    public DateOnly ReceivedDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public decimal QuantityReceived { get; set; }
    public decimal QuantityRemaining { get; set; }
    public decimal UnitCost { get; set; }

    public Guid? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public Guid? SupplierId { get; set; }
    public bool IsQuarantined { get; set; }
    public string? Notes { get; set; }

    public bool IsExpired =>
        ExpiryDate.HasValue && ExpiryDate.Value < DateOnly.FromDateTime(DateTime.Today);

    public bool IsExpiringSoon =>
        ExpiryDate.HasValue &&
        !IsExpired &&
        ExpiryDate.Value <= DateOnly.FromDateTime(DateTime.Today.AddDays(90));

    public int? DaysUntilExpiry =>
        ExpiryDate.HasValue
            ? ExpiryDate.Value.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber
            : null;
}

public class StockMovement : BaseEntity
{
    public Guid InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }
    public Guid? InventoryLotId { get; set; }
    public InventoryLot? InventoryLot { get; set; }

    public DateTime MovementDateUtc { get; set; } = DateTime.UtcNow;
    public StockMovementType MovementType { get; set; } = StockMovementType.Issue;

    /// <summary>Signed quantity: positive increases stock, negative decreases it.</summary>
    public decimal Quantity { get; set; }

    public decimal BalanceAfter { get; set; }
    public decimal? UnitCost { get; set; }

    public Guid? ProcedureId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public Guid? LocationId { get; set; }
    public Guid? PerformedByStaffId { get; set; }
    public string? Reference { get; set; }
    public string? Reason { get; set; }
    public string? Notes { get; set; }

    public decimal Value => Math.Abs(Quantity) * (UnitCost ?? 0m);
}

public class PurchaseOrder : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;

    public Guid SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public Guid? LocationId { get; set; }

    public DateOnly OrderDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly? ExpectedDate { get; set; }
    public DateOnly? ReceivedDate { get; set; }
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Draft;

    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Total { get; set; }

    public string? SupplierReference { get; set; }
    public string? TrackingNumber { get; set; }
    public string? OrderedBy { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public string? Notes { get; set; }

    public ICollection<PurchaseOrderLine> Lines { get; set; } = new List<PurchaseOrderLine>();

    public bool IsFullyReceived => Lines.Count > 0 && Lines.All(l => l.QuantityReceived >= l.QuantityOrdered);
    public bool IsOverdue =>
        Status is not (PurchaseOrderStatus.Received or PurchaseOrderStatus.Cancelled) &&
        ExpectedDate.HasValue && ExpectedDate.Value < DateOnly.FromDateTime(DateTime.Today);
}

public class PurchaseOrderLine : BaseEntity
{
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    public Guid InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }

    public int Sequence { get; set; }
    public decimal QuantityOrdered { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal UnitCost { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal TaxRatePercent { get; set; }
    public string? Notes { get; set; }

    public decimal Gross => QuantityOrdered * UnitCost;
    public decimal LineTotal => Math.Round(Gross * (1 - DiscountPercent / 100m), 2);
    public decimal QuantityOutstanding => QuantityOrdered - QuantityReceived;
}

// ------------------------------------------------------------------ Sterilisation

public class Steriliser : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public Guid? LocationId { get; set; }
    public Location? Location { get; set; }

    public DateOnly? InstallationDate { get; set; }
    public DateOnly? LastServiceDate { get; set; }
    public DateOnly? NextServiceDue { get; set; }
    public DateOnly? LastValidationDate { get; set; }
    public DateOnly? NextValidationDue { get; set; }
    public DateOnly? PressureVesselCertExpiry { get; set; }

    public bool IsActive { get; set; } = true;
    public int NextCycleNumber { get; set; } = 1;
    public string? Notes { get; set; }

    public ICollection<SterilisationCycle> Cycles { get; set; } = new List<SterilisationCycle>();

    public bool ServiceOverdue =>
        NextServiceDue.HasValue && NextServiceDue.Value < DateOnly.FromDateTime(DateTime.Today);
}

public class SterilisationCycle : BaseEntity
{
    public Guid SteriliserId { get; set; }
    public Steriliser? Steriliser { get; set; }

    public int CycleNumber { get; set; }
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public SterilisationProgram Program { get; set; } = SterilisationProgram.Vacuum134;

    public decimal? PeakTemperatureCelsius { get; set; }
    public decimal? PeakPressureBar { get; set; }
    public int? HoldTimeMinutes { get; set; }

    public bool ChemicalIndicatorPass { get; set; } = true;
    public bool? BiologicalIndicatorPass { get; set; }
    public DateOnly? BiologicalIndicatorReadDate { get; set; }
    public bool HelixTestPass { get; set; } = true;
    public bool VacuumLeakTestPass { get; set; } = true;
    public bool PrinterRecordAttached { get; set; }

    public SterilisationResult Result { get; set; } = SterilisationResult.Pending;
    public Guid? OperatorStaffId { get; set; }
    public Staff? OperatorStaff { get; set; }
    public int ItemCount { get; set; }
    public string? LoadContents { get; set; }
    public string? FailureReason { get; set; }
    public string? CorrectiveAction { get; set; }
    public string? Notes { get; set; }

    public ICollection<InstrumentSet> Sets { get; set; } = new List<InstrumentSet>();

    public int? DurationMinutes =>
        CompletedAtUtc.HasValue ? (int)(CompletedAtUtc.Value - StartedAtUtc).TotalMinutes : null;
}

public class InstrumentSet : BaseEntity
{
    public string SetCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Contents { get; set; }
    public int ItemCount { get; set; }
    public string? TrayType { get; set; }
    public Guid? LocationId { get; set; }

    public InstrumentSetStatus Status { get; set; } = InstrumentSetStatus.Sterile;
    public Guid? LastCycleId { get; set; }
    public SterilisationCycle? LastCycle { get; set; }
    public DateOnly? SterilisedOn { get; set; }
    public DateOnly? SterilityExpiryDate { get; set; }
    public int UsageCount { get; set; }
    public DateOnly? LastUsedOn { get; set; }
    public bool IsSurgicalSet { get; set; }
    public string? Notes { get; set; }

    public ICollection<InstrumentSetUsage> Usages { get; set; } = new List<InstrumentSetUsage>();

    public bool IsExpired =>
        SterilityExpiryDate.HasValue && SterilityExpiryDate.Value < DateOnly.FromDateTime(DateTime.Today);

    public bool IsAvailable => Status == InstrumentSetStatus.Sterile && !IsExpired;
}

public class InstrumentSetUsage : BaseEntity
{
    public Guid InstrumentSetId { get; set; }
    public InstrumentSet? InstrumentSet { get; set; }

    public Guid? ProcedureId { get; set; }
    public Guid? AppointmentId { get; set; }
    public Guid? PatientId { get; set; }
    public DateTime UsedAtUtc { get; set; } = DateTime.UtcNow;
    public Guid? UsedByStaffId { get; set; }
    public Guid? CycleIdAtTimeOfUse { get; set; }
    public string? Notes { get; set; }
}

// ------------------------------------------------------------------ Laboratory

public class DentalLaboratory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? AccountNumber { get; set; }
    public string? ContactPerson { get; set; }
    public ContactDetails Contact { get; set; } = new();
    public Address Address { get; set; } = new();
    public string? Specialities { get; set; }
    public int StandardTurnaroundDays { get; set; } = 10;
    public bool AcceptsDigitalImpressions { get; set; }
    public bool IsPreferred { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    public ICollection<LabCase> Cases { get; set; } = new List<LabCase>();
}

public class LabCase : BaseEntity
{
    public string CaseNumber { get; set; } = string.Empty;

    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public Guid DentalLaboratoryId { get; set; }
    public DentalLaboratory? DentalLaboratory { get; set; }

    public Guid? ProcedureId { get; set; }
    public Guid? TreatmentPlanItemId { get; set; }
    public Guid? ProviderId { get; set; }
    public Staff? Provider { get; set; }
    public Guid? DeliveryAppointmentId { get; set; }

    public LabCaseType CaseType { get; set; } = LabCaseType.Crown;
    public LabCaseStatus Status { get; set; } = LabCaseStatus.Draft;

    /// <summary>Comma-separated FDI tooth numbers.</summary>
    public string? ToothNumbers { get; set; }
    public DentalArch? Arch { get; set; }
    public string? Shade { get; set; }
    public string? ShadeGuide { get; set; }
    public RestorationMaterial Material { get; set; } = RestorationMaterial.Zirconia;
    public string? MaterialDetail { get; set; }
    public string? OcclusalScheme { get; set; }
    public string? PonticDesign { get; set; }
    public string? MarginDesign { get; set; }

    public bool DigitalImpression { get; set; }
    public bool PhysicalImpressionSent { get; set; }
    public bool BiteRegistrationSent { get; set; }
    public bool OppositingModelSent { get; set; }
    public bool PhotographsSent { get; set; }

    public DateOnly? SentDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateOnly? ReceivedDate { get; set; }
    public DateOnly? TryInDate { get; set; }
    public DateOnly? DeliveryDate { get; set; }

    public decimal? LabFee { get; set; }
    public decimal? PatientCharge { get; set; }
    public string? LabInvoiceNumber { get; set; }
    public string? TrackingNumber { get; set; }

    public bool IsRemake { get; set; }
    public Guid? RemakeOfCaseId { get; set; }
    public string? RemakeReason { get; set; }
    public bool QualityIssue { get; set; }

    public string? Instructions { get; set; }
    public string? Notes { get; set; }

    public bool IsOverdue =>
        Status is not (LabCaseStatus.Received or LabCaseStatus.Delivered or LabCaseStatus.Cancelled) &&
        DueDate.HasValue && DueDate.Value < DateOnly.FromDateTime(DateTime.Today);

    public int? DaysUntilDue =>
        DueDate.HasValue ? DueDate.Value.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber : null;
}

// ------------------------------------------------------------------ Communications

public class CommunicationLog : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public CommunicationChannel Channel { get; set; } = CommunicationChannel.Phone;
    public CommunicationDirection Direction { get; set; } = CommunicationDirection.Outbound;
    public CommunicationStatus Status { get; set; } = CommunicationStatus.Sent;

    public string? Subject { get; set; }
    public string? Body { get; set; }
    public string? Recipient { get; set; }
    public string? Sender { get; set; }

    public Guid? StaffId { get; set; }
    public Staff? Staff { get; set; }
    public Guid? AppointmentId { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid? RecallScheduleId { get; set; }
    public Guid? MessageTemplateId { get; set; }

    public bool RequiresFollowUp { get; set; }
    public DateOnly? FollowUpDate { get; set; }
    public string? Outcome { get; set; }
    public string? FailureReason { get; set; }
}

public class MessageTemplate : LookupEntity
{
    public CommunicationChannel Channel { get; set; } = CommunicationChannel.Email;
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? Category { get; set; }

    /// <summary>Comma-separated list of merge tokens, e.g. "{PatientName},{AppointmentTime}".</summary>
    public string? AvailableTokens { get; set; }
    public bool IsDefaultForCategory { get; set; }
}
