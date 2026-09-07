namespace DentalSurgery.Domain.Enums;

// ---------------------------------------------------------------- Scheduling

public enum AppointmentStatus
{
    Unconfirmed = 0, Confirmed = 1, ArrivedWaiting = 2, Seated = 3, InTreatment = 4,
    Completed = 5, CheckedOut = 6, Cancelled = 7, NoShow = 8, Rescheduled = 9, Broken = 10
}

public enum AppointmentType
{
    NewPatientExam = 0, RoutineExam = 1, Hygiene = 2, PeriodontalMaintenance = 3,
    Restorative = 4, Endodontic = 5, Extraction = 6, OralSurgery = 7, ImplantSurgery = 8,
    Prosthodontic = 9, Orthodontic = 10, Emergency = 11, Consultation = 12,
    Radiographs = 13, TreatmentPlanReview = 14, PostOperativeReview = 15,
    Whitening = 16, Paediatric = 17, LabTryIn = 18, Other = 99
}

public enum RecallType { RoutineExam = 0, ScaleAndPolish = 1, PeriodontalMaintenance = 2, Radiographs = 3, OrthodonticReview = 4, ImplantReview = 5, PostOperative = 6, Custom = 99 }

public enum RecallStatus { Scheduled = 0, Due = 1, Overdue = 2, Booked = 3, Completed = 4, Suspended = 5, OptedOut = 6 }

public enum WaitlistStatus { Waiting = 0, Contacted = 1, Booked = 2, Declined = 3, Expired = 4, Cancelled = 5 }

public enum WaitlistPriority { Low = 0, Normal = 1, High = 2, Urgent = 3 }

public enum ReminderStatus { Pending = 0, Sent = 1, Delivered = 2, Failed = 3, Confirmed = 4, CancelRequested = 5 }

// ---------------------------------------------------------------- Procedures & plans

public enum ProcedureCategory
{
    Diagnostic = 0, Preventive = 1, Restorative = 2, Endodontics = 3, Periodontics = 4,
    ProsthodonticsRemovable = 5, MaxillofacialProsthetics = 6, ImplantServices = 7,
    ProsthodonticsFixed = 8, OralAndMaxillofacialSurgery = 9, Orthodontics = 10,
    Adjunctive = 11, Cosmetic = 12, Paediatric = 13, Radiology = 14
}

public enum ProcedureStatus { Planned = 0, Scheduled = 1, InProgress = 2, Completed = 3, Cancelled = 4, Deferred = 5, Voided = 6, Referred = 7 }

public enum TreatmentPlanStatus { Draft = 0, Presented = 1, Accepted = 2, PartiallyAccepted = 3, Declined = 4, InProgress = 5, Completed = 6, Expired = 7, Superseded = 8 }

public enum TreatmentPlanItemStatus { Proposed = 0, Accepted = 1, Declined = 2, Scheduled = 3, InProgress = 4, Completed = 5, Deferred = 6, Removed = 7 }

public enum TreatmentPriority { Emergency = 0, Urgent = 1, High = 2, Routine = 3, Elective = 4, Monitor = 5 }

// ---------------------------------------------------------------- Billing

public enum InvoiceStatus { Draft = 0, Issued = 1, PartiallyPaid = 2, Paid = 3, Overdue = 4, Void = 5, WrittenOff = 6, Refunded = 7 }

public enum PaymentMethod { Cash = 0, CreditCard = 1, DebitCard = 2, BankTransfer = 3, Cheque = 4, DirectDebit = 5, Insurance = 6, FinancePlan = 7, AccountCredit = 8, Voucher = 9, Other = 99 }

public enum PaymentStatus { Pending = 0, Cleared = 1, Failed = 2, Refunded = 3, Reversed = 4, PartiallyRefunded = 5 }

public enum LedgerEntryType { Charge = 0, Payment = 1, InsurancePayment = 2, Adjustment = 3, Discount = 4, WriteOff = 5, Refund = 6, Transfer = 7, Interest = 8, LateFee = 9 }

public enum AdjustmentType { Discount = 0, CourtesyWriteOff = 1, BadDebt = 2, InsuranceWriteOff = 3, Correction = 4, StaffDiscount = 5, Promotional = 6, Goodwill = 7 }

public enum PaymentPlanStatus { Draft = 0, Active = 1, Completed = 2, Defaulted = 3, Cancelled = 4, OnHold = 5 }

public enum InstallmentStatus { Scheduled = 0, Paid = 1, PartiallyPaid = 2, Overdue = 3, Waived = 4, Failed = 5 }

public enum PaymentFrequency { Weekly = 0, Fortnightly = 1, Monthly = 2, Quarterly = 3 }

public enum InsurancePlanType { Ppo = 0, Hmo = 1, Indemnity = 2, DiscountPlan = 3, CapitationPlan = 4, NhsBand = 5, PrivateScheme = 6, Corporate = 7 }

public enum InsurancePriority { Primary = 0, Secondary = 1, Tertiary = 2 }

public enum ClaimStatus { Draft = 0, ReadyToSend = 1, Submitted = 2, Received = 3, InReview = 4, Approved = 5, PartiallyApproved = 6, Denied = 7, Paid = 8, Appealed = 9, Closed = 10 }

public enum FeeScheduleType { Practice = 0, Insurance = 1, Discount = 2, Capitation = 3, Nhs = 4 }

// ---------------------------------------------------------------- Inventory & sterilisation

public enum InventoryCategory
{
    Consumable = 0, Instrument = 1, Medication = 2, Anaesthetic = 3, ImplantComponent = 4,
    RestorativeMaterial = 5, ImpressionMaterial = 6, Ppe = 7, Sterilisation = 8,
    Radiography = 9, LaboratoryMaterial = 10, OfficeSupply = 11, Equipment = 12, Other = 99
}

public enum StockMovementType { Receipt = 0, Issue = 1, Adjustment = 2, Return = 3, Wastage = 4, Expiry = 5, Transfer = 6, StockTake = 7, Breakage = 8 }

public enum PurchaseOrderStatus { Draft = 0, Submitted = 1, Acknowledged = 2, PartiallyReceived = 3, Received = 4, Cancelled = 5, Disputed = 6 }

public enum SterilisationResult { Pending = 0, Pass = 1, Fail = 2, Quarantined = 3 }

public enum SterilisationProgram { Vacuum134 = 0, NonVacuum134 = 1, Vacuum121 = 2, Prion = 3, Delicate = 4, Rapid = 5 }

public enum InstrumentSetStatus { Sterile = 0, InUse = 1, Contaminated = 2, InProcessing = 3, Quarantined = 4, Retired = 5, Expired = 6 }

// ---------------------------------------------------------------- Laboratory

public enum LabCaseType
{
    Crown = 0, Bridge = 1, Veneer = 2, Inlay = 3, Onlay = 4, CompleteDenture = 5,
    PartialDenture = 6, ImplantAbutment = 7, ImplantCrown = 8, Nightguard = 9,
    SportsGuard = 10, Retainer = 11, ClearAligner = 12, StudyModel = 13,
    SurgicalGuide = 14, TemporaryRestoration = 15, Repair = 16, Reline = 17, Other = 99
}

public enum LabCaseStatus { Draft = 0, Sent = 1, InProduction = 2, Shipped = 3, Received = 4, TryIn = 5, Adjusting = 6, Delivered = 7, Remake = 8, Cancelled = 9 }

// ---------------------------------------------------------------- System

public enum AuditAction { Create = 0, Update = 1, Delete = 2, Read = 3, Login = 4, Logout = 5, LoginFailed = 6, Export = 7, Print = 8, PermissionChange = 9, Restore = 10 }
