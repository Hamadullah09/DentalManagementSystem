using DentalSurgery.Application.Abstractions;
using DentalSurgery.Application.Billing;
using DentalSurgery.Application.Common;
using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;
using DentalSurgery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalSurgery.Infrastructure.Services;

/// <summary>A patient's financial position, assembled for the account tab.</summary>
public class PatientAccount
{
    public Guid PatientId { get; init; }
    public decimal Balance { get; init; }
    public decimal InsurancePending { get; init; }
    public decimal TotalCharged { get; init; }
    public decimal TotalPaid { get; init; }
    public decimal TotalAdjusted { get; init; }
    public decimal UnallocatedCredit { get; init; }
    public AgingReport Aging { get; init; } = new();
    public IReadOnlyList<Invoice> Invoices { get; init; } = Array.Empty<Invoice>();
    public IReadOnlyList<Payment> Payments { get; init; } = Array.Empty<Payment>();
    public IReadOnlyList<LedgerEntry> Ledger { get; init; } = Array.Empty<LedgerEntry>();
    public IReadOnlyList<PaymentPlan> PaymentPlans { get; init; } = Array.Empty<PaymentPlan>();
    public IReadOnlyList<InsuranceClaim> Claims { get; init; } = Array.Empty<InsuranceClaim>();

    public bool IsInCredit => Balance < 0;
    public bool HasOverdueInvoices => Invoices.Any(i => i.IsOverdue);
}

/// <summary>Invoicing, payments, adjustments, ledger and insurance claims.</summary>
public class BillingService(
    DentalDbContext db,
    INumberSequenceService sequences,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    ILogger<BillingService> logger,
    IPermissionGuard guard)
{
    private readonly LedgerCalculator _ledger = new();
    private readonly InsuranceEstimator _estimator = new();

    // ------------------------------------------------------------------ account

    public async Task<PatientAccount> GetAccountAsync(Guid patientId, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.BillingView, ct);
        var invoices = await db.Invoices.AsNoTracking()
            .Include(i => i.Lines)
            .Where(i => i.PatientId == patientId)
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync(ct);

        var payments = await db.Payments.AsNoTracking()
            .Include(p => p.Allocations)
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync(ct);

        var ledger = await db.LedgerEntries.AsNoTracking()
            .Where(l => l.PatientId == patientId)
            .OrderByDescending(l => l.EntryDate).ThenByDescending(l => l.CreatedAtUtc)
            .ToListAsync(ct);

        var plans = await db.PaymentPlans.AsNoTracking()
            .Include(p => p.Installments)
            .Where(p => p.PatientId == patientId)
            .ToListAsync(ct);

        var claims = await db.InsuranceClaims.AsNoTracking()
            .Include(c => c.PatientInsurance).ThenInclude(i => i!.InsurancePlan)
            .Include(c => c.Lines)
            .Where(c => c.PatientId == patientId)
            .OrderByDescending(c => c.ServiceDate)
            .ToListAsync(ct);

        var adjustments = await db.AccountAdjustments.AsNoTracking()
            .Where(a => a.PatientId == patientId)
            .SumAsync(a => (decimal?)a.Amount, ct) ?? 0m;

        var balance = ledger.Count == 0 ? 0m : ledger.Sum(l => l.Debit - l.Credit);

        return new PatientAccount
        {
            PatientId = patientId,
            Balance = Math.Round(balance, 2),
            InsurancePending = claims
                .Where(c => c.Status is not (ClaimStatus.Paid or ClaimStatus.Closed or ClaimStatus.Denied))
                .Sum(c => c.TotalCharged - c.TotalPaid),
            TotalCharged = invoices.Where(i => i.Status != InvoiceStatus.Void).Sum(i => i.Total),
            TotalPaid = payments.Where(p => p.Status == PaymentStatus.Cleared).Sum(p => p.Amount - p.RefundedAmount),
            TotalAdjusted = adjustments,
            UnallocatedCredit = payments.Where(p => p.Status == PaymentStatus.Cleared).Sum(p => p.UnallocatedAmount),
            Aging = _ledger.BuildAging(invoices, clock.Today),
            Invoices = invoices,
            Payments = payments,
            Ledger = ledger,
            PaymentPlans = plans,
            Claims = claims
        };
    }

    // ------------------------------------------------------------------ invoicing

    /// <summary>
    /// Raises (or extends) an invoice for a completed procedure and posts the
    /// charge to the ledger. Charges made on the same day are grouped onto one
    /// draft invoice so the patient receives a single document.
    /// </summary>
    public async Task<Result<Invoice>> ChargeProcedureAsync(Guid procedureId, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.BillingCreate, ct);
        var procedure = await db.Procedures
            .Include(p => p.ProcedureCode)
            .Include(p => p.Tooth)
            .FirstOrDefaultAsync(p => p.Id == procedureId, ct);

        if (procedure is null) return Result<Invoice>.Failure("Procedure not found.");
        if (!procedure.IsBillable) return Result<Invoice>.Failure("This procedure is not billable.");
        if (procedure.InvoiceLineId is not null) return Result<Invoice>.Failure("This procedure is already invoiced.");

        var serviceDate = DateOnly.FromDateTime(procedure.DateOfService);

        var invoice = await db.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.PatientId == procedure.PatientId &&
                                      i.IssueDate == serviceDate &&
                                      i.Status == InvoiceStatus.Draft, ct);

        if (invoice is null)
        {
            var practice = await db.Practices.AsNoTracking().FirstOrDefaultAsync(ct);
            invoice = new Invoice
            {
                PatientId = procedure.PatientId,
                InvoiceNumber = await sequences.NextAsync(SequenceNames.Invoice, ct),
                IssueDate = serviceDate,
                DueDate = serviceDate.AddDays(practice?.InvoicePaymentTermDays ?? 30),
                ProviderId = procedure.ProviderId,
                LocationId = procedure.LocationId,
                Status = InvoiceStatus.Draft,
                TermsText = practice?.InvoiceFooterText
            };
            db.Invoices.Add(invoice);
        }

        var estimate = await EstimateCoverageAsync(procedure.PatientId, procedure.ProcedureCode!, procedure.NetFee, ct);

        var line = new InvoiceLine
        {
            InvoiceId = invoice.Id,
            ProcedureId = procedure.Id,
            ProcedureCodeId = procedure.ProcedureCodeId,
            ToothId = procedure.ToothId,
            Sequence = invoice.Lines.Count + 1,
            Description = procedure.Description,
            ServiceDate = serviceDate,
            Quantity = procedure.Quantity,
            UnitPrice = procedure.Fee,
            DiscountAmount = procedure.DiscountAmount,
            TaxRatePercent = procedure.ProcedureCode!.IsTaxable ? await TaxRateAsync(ct) : 0m,
            InsurancePortion = estimate.InsurancePortion,
            PatientPortion = estimate.PatientPortion,
            ProviderId = procedure.ProviderId
        };

        invoice.Lines.Add(line);
        procedure.InvoiceLineId = line.Id;

        RecalculateInvoice(invoice);
        await db.SaveChangesAsync(ct);

        await PostLedgerAsync(new LedgerEntry
        {
            PatientId = procedure.PatientId,
            EntryDate = serviceDate,
            EntryType = LedgerEntryType.Charge,
            Description = procedure.Description,
            Debit = line.LineTotal,
            InvoiceId = invoice.Id,
            ProcedureId = procedure.Id,
            ProviderId = procedure.ProviderId,
            Reference = invoice.InvoiceNumber
        }, ct);

        return Result<Invoice>.Success(invoice);
    }

    public async Task<Result> ReverseProcedureChargeAsync(
        Guid procedureId, string reason, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.BillingEdit, ct);
        var procedure = await db.Procedures.FirstOrDefaultAsync(p => p.Id == procedureId, ct);
        if (procedure?.InvoiceLineId is null) return Result.Success();

        var line = await db.InvoiceLines
            .Include(l => l.Invoice).ThenInclude(i => i!.Lines)
            .FirstOrDefaultAsync(l => l.Id == procedure.InvoiceLineId, ct);

        if (line?.Invoice is null) return Result.Success();

        var amount = line.LineTotal;
        var invoice = line.Invoice;

        if (invoice.Status == InvoiceStatus.Draft)
        {
            invoice.Lines.Remove(line);
            db.InvoiceLines.Remove(line);
            RecalculateInvoice(invoice);
        }
        else
        {
            // An issued invoice is corrected with an adjustment, never edited.
            db.AccountAdjustments.Add(new AccountAdjustment
            {
                PatientId = procedure.PatientId,
                AdjustmentDate = clock.Today,
                AdjustmentType = AdjustmentType.Correction,
                Amount = amount,
                InvoiceId = invoice.Id,
                Reason = $"Reversal of voided procedure: {reason}",
                ApprovedBy = currentUser.DisplayName
            });
            invoice.WriteOffAmount += amount;
            UpdateInvoiceStatus(invoice);
        }

        procedure.InvoiceLineId = null;
        await db.SaveChangesAsync(ct);

        await PostLedgerAsync(new LedgerEntry
        {
            PatientId = procedure.PatientId,
            EntryDate = clock.Today,
            EntryType = LedgerEntryType.Adjustment,
            Description = $"Reversal: {procedure.Description}",
            Credit = amount,
            InvoiceId = invoice.Id,
            ProcedureId = procedureId,
            Reference = invoice.InvoiceNumber
        }, ct);

        return Result.Success();
    }

    public async Task<Result<Invoice>> IssueInvoiceAsync(Guid invoiceId, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.BillingCreate, ct);
        var invoice = await db.Invoices.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == invoiceId, ct);
        if (invoice is null) return Result<Invoice>.Failure("Invoice not found.");
        if (invoice.Status != InvoiceStatus.Draft) return Result<Invoice>.Failure("This invoice has already been issued.");
        if (invoice.Lines.Count == 0) return Result<Invoice>.Failure("The invoice has no lines.");

        RecalculateInvoice(invoice);
        invoice.Status = invoice.AmountPaid >= invoice.Total ? InvoiceStatus.Paid : InvoiceStatus.Issued;

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Issued invoice {Number} for {Total}.", invoice.InvoiceNumber, invoice.Total);
        return Result<Invoice>.Success(invoice);
    }

    public async Task<Result> VoidInvoiceAsync(Guid invoiceId, string reason, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.BillingEdit, ct);
        var invoice = await db.Invoices.Include(i => i.Lines).FirstOrDefaultAsync(i => i.Id == invoiceId, ct);
        if (invoice is null) return Result.Failure("Invoice not found.");
        if (invoice.AmountPaid > 0) return Result.Failure("A paid invoice cannot be voided. Refund it instead.");

        invoice.Status = InvoiceStatus.Void;
        invoice.VoidedAtUtc = clock.UtcNow;
        invoice.VoidReason = reason;

        foreach (var line in invoice.Lines)
        {
            var procedure = await db.Procedures.FirstOrDefaultAsync(p => p.InvoiceLineId == line.Id, ct);
            if (procedure is not null) procedure.InvoiceLineId = null;
        }

        await db.SaveChangesAsync(ct);

        await PostLedgerAsync(new LedgerEntry
        {
            PatientId = invoice.PatientId,
            EntryDate = clock.Today,
            EntryType = LedgerEntryType.Adjustment,
            Description = $"Invoice {invoice.InvoiceNumber} voided: {reason}",
            Credit = invoice.Total,
            InvoiceId = invoice.Id,
            Reference = invoice.InvoiceNumber
        }, ct);

        return Result.Success();
    }

    // ------------------------------------------------------------------ payments

    public async Task<Result<Payment>> TakePaymentAsync(
        Guid patientId, decimal amount, PaymentMethod method,
        string? reference = null, IEnumerable<Guid>? invoiceIds = null,
        CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.BillingCreate, ct);
        if (amount <= 0) return Result<Payment>.Failure("The payment amount must be greater than zero.");

        var payment = new Payment
        {
            PatientId = patientId,
            PaymentNumber = await sequences.NextAsync(SequenceNames.Payment, ct),
            PaymentDate = clock.Today,
            Amount = amount,
            Method = method,
            Status = PaymentStatus.Cleared,
            ReferenceNumber = reference,
            ReceivedBy = currentUser.DisplayName ?? currentUser.UserName
        };

        db.Payments.Add(payment);

        // Allocate to the invoices given, or oldest-first across the account.
        var candidates = await db.Invoices
            .Include(i => i.Lines)
            .Where(i => i.PatientId == patientId &&
                        i.Status != InvoiceStatus.Void &&
                        i.Status != InvoiceStatus.Draft &&
                        i.Status != InvoiceStatus.WrittenOff)
            .ToListAsync(ct);

        if (invoiceIds is not null)
        {
            var wanted = invoiceIds.ToHashSet();
            candidates = candidates.Where(i => wanted.Contains(i.Id)).ToList();
        }

        var (allocations, unallocated) = _ledger.AllocateOldestFirst(amount, candidates);

        foreach (var (invoice, applied) in allocations)
        {
            db.PaymentAllocations.Add(new PaymentAllocation
            {
                PaymentId = payment.Id,
                InvoiceId = invoice.Id,
                Amount = applied,
                AllocatedOn = clock.Today
            });

            invoice.AmountPaid += applied;
            UpdateInvoiceStatus(invoice);
        }

        await db.SaveChangesAsync(ct);

        await PostLedgerAsync(new LedgerEntry
        {
            PatientId = patientId,
            EntryDate = clock.Today,
            EntryType = method == PaymentMethod.Insurance ? LedgerEntryType.InsurancePayment : LedgerEntryType.Payment,
            Description = $"Payment received by {method}" +
                          (unallocated > 0 ? $" ({unallocated:0.00} held on account)" : string.Empty),
            Credit = amount,
            PaymentId = payment.Id,
            Reference = payment.PaymentNumber
        }, ct);

        logger.LogInformation("Recorded payment {Number} of {Amount} across {Count} invoices.",
            payment.PaymentNumber, amount, allocations.Count);

        return Result<Payment>.Success(payment);
    }

    public async Task<Result> RefundPaymentAsync(
        Guid paymentId, decimal amount, string reason, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.BillingRefund, ct);
        var payment = await db.Payments.Include(p => p.Allocations).FirstOrDefaultAsync(p => p.Id == paymentId, ct);
        if (payment is null) return Result.Failure("Payment not found.");

        var refundable = payment.Amount - payment.RefundedAmount;
        if (amount <= 0 || amount > refundable)
            return Result.Failure($"The refund must be between 0.01 and {refundable:0.00}.");

        payment.RefundedAmount += amount;
        payment.RefundedOn = clock.Today;
        payment.RefundReason = reason;
        payment.Status = payment.RefundedAmount >= payment.Amount
            ? PaymentStatus.Refunded
            : PaymentStatus.PartiallyRefunded;

        // Unwind the allocations so the invoices show as owing again.
        var remaining = amount;
        foreach (var allocation in payment.Allocations.OrderByDescending(a => a.Amount))
        {
            if (remaining <= 0) break;
            var reversal = Math.Min(remaining, allocation.Amount);
            var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == allocation.InvoiceId, ct);
            if (invoice is not null)
            {
                invoice.AmountPaid -= reversal;
                UpdateInvoiceStatus(invoice);
            }
            allocation.Amount -= reversal;
            remaining -= reversal;
        }

        await db.SaveChangesAsync(ct);

        await PostLedgerAsync(new LedgerEntry
        {
            PatientId = payment.PatientId,
            EntryDate = clock.Today,
            EntryType = LedgerEntryType.Refund,
            Description = $"Refund: {reason}",
            Debit = amount,
            PaymentId = payment.Id,
            Reference = payment.PaymentNumber
        }, ct);

        return Result.Success();
    }

    public async Task<Result<AccountAdjustment>> AdjustAsync(
        Guid patientId, decimal amount, AdjustmentType type, string reason,
        Guid? invoiceId = null, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.BillingEdit, ct);
        if (amount == 0) return Result<AccountAdjustment>.Failure("The adjustment amount cannot be zero.");
        if (string.IsNullOrWhiteSpace(reason)) return Result<AccountAdjustment>.Failure("A reason is required.");

        var adjustment = new AccountAdjustment
        {
            PatientId = patientId,
            AdjustmentDate = clock.Today,
            AdjustmentType = type,
            Amount = amount,
            InvoiceId = invoiceId,
            Reason = reason,
            ApprovedBy = currentUser.DisplayName ?? currentUser.UserName,
            ApprovedAtUtc = clock.UtcNow
        };

        db.AccountAdjustments.Add(adjustment);

        if (invoiceId.HasValue)
        {
            var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId, ct);
            if (invoice is not null)
            {
                invoice.WriteOffAmount += amount;
                UpdateInvoiceStatus(invoice);
            }
        }

        await db.SaveChangesAsync(ct);

        await PostLedgerAsync(new LedgerEntry
        {
            PatientId = patientId,
            EntryDate = clock.Today,
            EntryType = type == AdjustmentType.Discount ? LedgerEntryType.Discount : LedgerEntryType.Adjustment,
            Description = $"{type}: {reason}",
            Credit = amount > 0 ? amount : 0m,
            Debit = amount < 0 ? -amount : 0m,
            InvoiceId = invoiceId,
            AdjustmentId = adjustment.Id
        }, ct);

        return Result<AccountAdjustment>.Success(adjustment);
    }

    // ------------------------------------------------------------------ payment plans

    public async Task<Result<PaymentPlan>> CreatePaymentPlanAsync(
        PaymentPlan plan, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.BillingEdit, ct);
        if (plan.TotalAmount <= 0) return Result<PaymentPlan>.Failure("The plan total must be greater than zero.");
        if (plan.NumberOfInstallments <= 0) return Result<PaymentPlan>.Failure("Set the number of instalments.");

        plan.PlanNumber = await sequences.NextAsync(SequenceNames.PaymentPlan, ct);
        plan.Status = PaymentPlanStatus.Active;

        var installments = _ledger.BuildInstallments(plan);
        plan.InstallmentAmount = installments.Count > 0 ? installments[0].AmountDue : 0m;
        plan.EndDate = installments.Count > 0 ? installments[^1].DueDate : plan.StartDate;
        foreach (var installment in installments) plan.Installments.Add(installment);

        db.PaymentPlans.Add(plan);
        await db.SaveChangesAsync(ct);
        return Result<PaymentPlan>.Success(plan);
    }

    // ------------------------------------------------------------------ insurance claims

    public async Task<Result<InsuranceClaim>> CreateClaimAsync(
        Guid patientId, Guid patientInsuranceId, IEnumerable<Guid> procedureIds,
        bool isPreAuthorisation = false, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.InsuranceCreate, ct);
        var insurance = await db.PatientInsurances
            .Include(i => i.InsurancePlan).ThenInclude(p => p!.InsuranceCarrier)
            .FirstOrDefaultAsync(i => i.Id == patientInsuranceId, ct);

        if (insurance is null) return Result<InsuranceClaim>.Failure("Insurance policy not found.");
        if (!insurance.IsCurrentlyValid) return Result<InsuranceClaim>.Failure("The policy is not currently active.");

        var ids = procedureIds.ToList();
        var procedures = await db.Procedures
            .Include(p => p.ProcedureCode).Include(p => p.Tooth)
            .Where(p => ids.Contains(p.Id) && p.PatientId == patientId)
            .ToListAsync(ct);

        if (procedures.Count == 0) return Result<InsuranceClaim>.Failure("No billable procedures were selected.");

        var claim = new InsuranceClaim
        {
            PatientId = patientId,
            PatientInsuranceId = patientInsuranceId,
            ClaimNumber = await sequences.NextAsync(SequenceNames.Claim, ct),
            IsPreAuthorisation = isPreAuthorisation,
            ServiceDate = DateOnly.FromDateTime(procedures.Min(p => p.DateOfService)),
            Status = ClaimStatus.Draft,
            ProviderId = procedures[0].ProviderId
        };

        var sequence = 1;
        foreach (var procedure in procedures)
        {
            var estimate = _estimator.Estimate(procedure.ProcedureCode!, procedure.NetFee, insurance);

            claim.Lines.Add(new InsuranceClaimLine
            {
                InsuranceClaimId = claim.Id,
                ProcedureId = procedure.Id,
                ProcedureCodeId = procedure.ProcedureCodeId,
                ToothId = procedure.ToothId,
                SurfaceCode = procedure.Surfaces == ToothSurface.None ? null : SurfaceNotation.ToCode(procedure.Surfaces),
                ServiceDate = DateOnly.FromDateTime(procedure.DateOfService),
                Sequence = sequence++,
                ChargedAmount = procedure.NetFee,
                AllowedAmount = estimate.AllowedAmount,
                DeductibleAmount = estimate.DeductibleApplied
            });
        }

        claim.TotalCharged = claim.Lines.Sum(l => l.ChargedAmount);
        claim.TotalAllowed = claim.Lines.Sum(l => l.AllowedAmount);
        claim.DeductibleApplied = claim.Lines.Sum(l => l.DeductibleAmount);

        db.InsuranceClaims.Add(claim);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Created {Type} {Number} for {Amount}.",
            isPreAuthorisation ? "pre-authorisation" : "claim", claim.ClaimNumber, claim.TotalCharged);

        return Result<InsuranceClaim>.Success(claim);
    }

    public async Task<Result> SubmitClaimAsync(Guid claimId, string method, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.InsuranceSubmitClaim, ct);
        var claim = await db.InsuranceClaims.FirstOrDefaultAsync(c => c.Id == claimId, ct);
        if (claim is null) return Result.Failure("Claim not found.");
        if (claim.Status is not (ClaimStatus.Draft or ClaimStatus.ReadyToSend))
            return Result.Failure("Only a draft claim can be submitted.");

        claim.Status = ClaimStatus.Submitted;
        claim.SubmittedOn = clock.Today;
        claim.SubmissionMethod = method;

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <summary>Applies an insurer remittance, posting the payment and any write-off.</summary>
    public async Task<Result> RecordClaimPaymentAsync(
        Guid claimId, decimal amountPaid, decimal writeOff, string? denialReason,
        CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.InsuranceCreate, ct);
        var claim = await db.InsuranceClaims
            .Include(c => c.Lines)
            .Include(c => c.PatientInsurance)
            .FirstOrDefaultAsync(c => c.Id == claimId, ct);

        if (claim is null) return Result.Failure("Claim not found.");

        claim.TotalPaid += amountPaid;
        claim.WriteOffAmount += writeOff;
        claim.AdjudicatedOn = clock.Today;
        claim.PaidOn = amountPaid > 0 ? clock.Today : null;
        claim.DenialReason = denialReason;

        claim.PatientResponsibility = Math.Max(0m,
            claim.TotalCharged - claim.TotalPaid - claim.WriteOffAmount);

        claim.Status = amountPaid <= 0
            ? ClaimStatus.Denied
            : claim.TotalPaid >= claim.TotalCharged - claim.WriteOffAmount
                ? ClaimStatus.Paid
                : ClaimStatus.PartiallyApproved;

        if (claim.PatientInsurance is not null && amountPaid > 0)
            claim.PatientInsurance.BenefitsUsedThisYear += amountPaid;

        await db.SaveChangesAsync(ct);

        if (amountPaid > 0)
        {
            await TakePaymentAsync(claim.PatientId, amountPaid, PaymentMethod.Insurance,
                $"Claim {claim.ClaimNumber}", null, ct);

            var payment = await db.Payments
                .Where(p => p.PatientId == claim.PatientId && p.ReferenceNumber == $"Claim {claim.ClaimNumber}")
                .OrderByDescending(p => p.CreatedAtUtc)
                .FirstOrDefaultAsync(ct);

            if (payment is not null)
            {
                payment.InsuranceClaimId = claim.Id;
                await db.SaveChangesAsync(ct);
            }
        }

        if (writeOff > 0)
        {
            await AdjustAsync(claim.PatientId, writeOff, AdjustmentType.InsuranceWriteOff,
                $"Contractual write-off on claim {claim.ClaimNumber}", claim.InvoiceId, ct);
        }

        return Result.Success();
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>Appends a ledger row and refreshes the patient's cached balance.</summary>
    private async Task PostLedgerAsync(LedgerEntry entry, CancellationToken ct)
    {
        var previous = await db.LedgerEntries
            .Where(l => l.PatientId == entry.PatientId)
            .OrderByDescending(l => l.EntryDate).ThenByDescending(l => l.CreatedAtUtc)
            .Select(l => (decimal?)l.RunningBalance)
            .FirstOrDefaultAsync(ct) ?? 0m;

        entry.RunningBalance = Math.Round(previous + entry.Debit - entry.Credit, 2);
        db.LedgerEntries.Add(entry);

        var patient = await db.Patients.FirstOrDefaultAsync(p => p.Id == entry.PatientId, ct);
        if (patient is not null) patient.AccountBalance = entry.RunningBalance;

        await db.SaveChangesAsync(ct);
    }

    private static void RecalculateInvoice(Invoice invoice)
    {
        invoice.Subtotal = invoice.Lines.Sum(l => l.Gross);
        invoice.DiscountAmount = invoice.Lines.Sum(l => l.DiscountAmount);
        invoice.TaxAmount = invoice.Lines.Sum(l => l.TaxAmount);
        invoice.Total = Math.Round(invoice.Lines.Sum(l => l.LineTotal), 2);
        invoice.InsuranceEstimate = invoice.Lines.Sum(l => l.InsurancePortion);
        UpdateInvoiceStatus(invoice);
    }

    private static void UpdateInvoiceStatus(Invoice invoice)
    {
        if (invoice.Status is InvoiceStatus.Void or InvoiceStatus.Draft) return;

        var settled = invoice.AmountPaid + invoice.WriteOffAmount;

        if (settled >= invoice.Total - 0.005m)
        {
            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidInFullOn ??= DateOnly.FromDateTime(DateTime.Today);
        }
        else if (settled > 0)
        {
            invoice.Status = InvoiceStatus.PartiallyPaid;
            invoice.PaidInFullOn = null;
        }
        else
        {
            invoice.Status = invoice.DueDate < DateOnly.FromDateTime(DateTime.Today)
                ? InvoiceStatus.Overdue
                : InvoiceStatus.Issued;
            invoice.PaidInFullOn = null;
        }
    }

    private async Task<CoverageEstimate> EstimateCoverageAsync(
        Guid patientId, ProcedureCode code, decimal fee, CancellationToken ct)
    {
        var policies = await db.PatientInsurances.AsNoTracking()
            .Include(p => p.InsurancePlan)
            .Where(p => p.PatientId == patientId && p.IsActive)
            .ToListAsync(ct);

        return _estimator.EstimateCoordinated(code, fee, policies);
    }

    private async Task<decimal> TaxRateAsync(CancellationToken ct)
    {
        var practice = await db.Practices.AsNoTracking().FirstOrDefaultAsync(ct);
        return practice?.DefaultTaxRatePercent ?? 0m;
    }

    /// <summary>Rebuilds a patient's ledger balances after a data correction.</summary>
    /// <summary>
    /// Recomputes a patient's running balance from their charges and payments.
    /// <para>
    /// No permission demand: this is an internal repair called by the billing
    /// operations that have already been authorised, and by nothing a user can
    /// reach directly. Demanding a permission here would make it fail inside a
    /// legitimately authorised operation whose own permission differs.
    /// </para>
    /// </summary>
    public async Task<decimal> RebuildLedgerAsync(Guid patientId, CancellationToken ct = default)
    {
        var entries = await db.LedgerEntries
            .Where(l => l.PatientId == patientId)
            .OrderBy(l => l.EntryDate).ThenBy(l => l.CreatedAtUtc)
            .ToListAsync(ct);

        var balance = _ledger.Recalculate(entries);

        var patient = await db.Patients.FirstOrDefaultAsync(p => p.Id == patientId, ct);
        if (patient is not null) patient.AccountBalance = balance;

        await db.SaveChangesAsync(ct);
        return balance;
    }
}
