using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;

namespace DentalSurgery.Application.Billing;

/// <summary>Split of one procedure's fee between insurer and patient.</summary>
public record CoverageEstimate(
    decimal GrossFee,
    decimal AllowedAmount,
    decimal DeductibleApplied,
    decimal InsurancePortion,
    decimal PatientPortion,
    decimal CoveragePercent,
    string? Explanation = null)
{
    public static CoverageEstimate SelfPay(decimal fee) =>
        new(fee, fee, 0m, 0m, fee, 0m, "No active insurance; the patient pays in full.");
}

/// <summary>Resolves the price of a procedure against the applicable fee schedule.</summary>
public class FeeCalculator
{
    public decimal ResolveFee(ProcedureCode code, FeeSchedule? schedule, IEnumerable<FeeScheduleItem>? items = null)
    {
        if (schedule is null) return code.DefaultFee;

        var pool = items ?? schedule.Items;
        var match = pool.FirstOrDefault(i => i.ProcedureCodeId == code.Id && i.FeeScheduleId == schedule.Id);
        if (match is not null) return match.Fee;

        if (schedule.BlanketAdjustmentPercent is { } pct)
            return Math.Round(code.DefaultFee * (1 + pct / 100m), 2);

        return code.DefaultFee;
    }

    /// <summary>Total for a line, applying quantity, discount and tax in that order.</summary>
    public decimal LineTotal(decimal unitFee, decimal quantity, decimal discountAmount, decimal taxRatePercent)
    {
        var gross = unitFee * quantity;
        var net = Math.Max(0m, gross - discountAmount);
        var tax = Math.Round(net * taxRatePercent / 100m, 2);
        return Math.Round(net + tax, 2);
    }
}

/// <summary>
/// Estimates what an insurer is likely to pay. The result is an estimate only:
/// benefits are confirmed by the payer at adjudication.
/// </summary>
public class InsuranceEstimator
{
    public CoverageEstimate Estimate(
        ProcedureCode code,
        decimal grossFee,
        PatientInsurance? insurance,
        decimal? allowedAmountOverride = null)
    {
        if (insurance is null || !insurance.IsCurrentlyValid || insurance.InsurancePlan is null)
            return CoverageEstimate.SelfPay(grossFee);

        var plan = insurance.InsurancePlan;
        var allowed = allowedAmountOverride ?? grossFee;
        var coveragePercent = plan.CoverageFor(code.Category);

        // Waiting periods
        var monthsEnrolled = MonthsBetween(insurance.EffectiveFrom, DateOnly.FromDateTime(DateTime.Today));
        var waiting = code.Category switch
        {
            ProcedureCategory.Restorative or ProcedureCategory.Endodontics or ProcedureCategory.Periodontics
                => plan.WaitingPeriodBasicMonths,
            ProcedureCategory.ProsthodonticsFixed or ProcedureCategory.ProsthodonticsRemovable
                or ProcedureCategory.ImplantServices => plan.WaitingPeriodMajorMonths,
            ProcedureCategory.Orthodontics => plan.WaitingPeriodOrthoMonths,
            _ => null
        };

        if (waiting.HasValue && monthsEnrolled < waiting.Value)
        {
            return new CoverageEstimate(grossFee, allowed, 0m, 0m, grossFee, 0m,
                $"Within the {waiting.Value}-month waiting period for this benefit category.");
        }

        // Deductible
        var deductibleApplied = 0m;
        var isPreventiveOrDiagnostic = code.Category is ProcedureCategory.Preventive
            or ProcedureCategory.Diagnostic or ProcedureCategory.Radiology;

        if (plan.IndividualDeductible is { } deductible &&
            (!isPreventiveOrDiagnostic || plan.DeductibleAppliesToPreventive))
        {
            var remaining = Math.Max(0m, deductible - insurance.DeductibleMetThisYear);
            deductibleApplied = Math.Min(remaining, allowed);
        }

        var coverable = Math.Max(0m, allowed - deductibleApplied);
        var insurancePortion = Math.Round(coverable * coveragePercent / 100m, 2);

        // Annual maximum
        var explanation = (string?)null;
        if (plan.AnnualMaximum is { } annualMax)
        {
            var remainingBenefit = Math.Max(0m, annualMax - insurance.BenefitsUsedThisYear);
            if (insurancePortion > remainingBenefit)
            {
                insurancePortion = remainingBenefit;
                explanation = remainingBenefit <= 0m
                    ? "The annual maximum has been reached; no further benefit is available this year."
                    : $"Capped at the remaining annual maximum of {remainingBenefit:0.00}.";
            }
        }

        var patientPortion = Math.Round(grossFee - insurancePortion, 2);

        return new CoverageEstimate(
            grossFee, allowed, deductibleApplied, insurancePortion, patientPortion, coveragePercent,
            explanation ?? $"{coveragePercent:0}% of the allowed amount under {plan.PlanName}.");
    }

    /// <summary>Applies primary then secondary cover, so the patient pays the remainder once.</summary>
    public CoverageEstimate EstimateCoordinated(
        ProcedureCode code,
        decimal grossFee,
        IEnumerable<PatientInsurance> policies)
    {
        var ordered = policies
            .Where(p => p.IsCurrentlyValid)
            .OrderBy(p => p.Priority)
            .ToList();

        if (ordered.Count == 0) return CoverageEstimate.SelfPay(grossFee);

        var totalInsurance = 0m;
        var totalDeductible = 0m;
        var remaining = grossFee;
        var notes = new List<string>();
        decimal firstCoverage = 0m;

        foreach (var policy in ordered)
        {
            if (remaining <= 0m) break;
            var estimate = Estimate(code, remaining, policy);
            totalInsurance += estimate.InsurancePortion;
            totalDeductible += estimate.DeductibleApplied;
            remaining = Math.Max(0m, remaining - estimate.InsurancePortion);
            if (firstCoverage == 0m) firstCoverage = estimate.CoveragePercent;
            if (estimate.Explanation is not null)
                notes.Add($"{policy.Priority}: {estimate.Explanation}");
        }

        totalInsurance = Math.Min(totalInsurance, grossFee);

        return new CoverageEstimate(
            grossFee, grossFee, totalDeductible, totalInsurance,
            Math.Round(grossFee - totalInsurance, 2), firstCoverage,
            notes.Count > 0 ? string.Join(" ", notes) : null);
    }

    private static int MonthsBetween(DateOnly from, DateOnly to) =>
        ((to.Year - from.Year) * 12) + to.Month - from.Month;
}

/// <summary>One bucket of the accounts-receivable ageing report.</summary>
public record AgingBucket(string Label, decimal Amount, int InvoiceCount)
{
    public bool IsEmpty => InvoiceCount == 0;
}

public class AgingReport
{
    public decimal Current { get; init; }
    public decimal Days1To30 { get; init; }
    public decimal Days31To60 { get; init; }
    public decimal Days61To90 { get; init; }
    public decimal Over90Days { get; init; }
    public decimal Total => Current + Days1To30 + Days31To60 + Days61To90 + Over90Days;

    public IReadOnlyList<AgingBucket> Buckets { get; init; } = Array.Empty<AgingBucket>();

    public decimal PercentOver90 => Total == 0m ? 0m : Math.Round(100m * Over90Days / Total, 1);
}

/// <summary>Builds patient balances and the ageing report from ledger data.</summary>
public class LedgerCalculator
{
    /// <summary>Recomputes running balances over an ordered ledger, returning the closing balance.</summary>
    public decimal Recalculate(IEnumerable<LedgerEntry> entriesInOrder)
    {
        var balance = 0m;
        foreach (var entry in entriesInOrder.OrderBy(e => e.EntryDate).ThenBy(e => e.CreatedAtUtc))
        {
            balance += entry.Debit - entry.Credit;
            entry.RunningBalance = Math.Round(balance, 2);
        }
        return Math.Round(balance, 2);
    }

    public AgingReport BuildAging(IEnumerable<Invoice> invoices, DateOnly? asOf = null)
    {
        var today = asOf ?? DateOnly.FromDateTime(DateTime.Today);

        var outstanding = invoices
            .Where(i => i.Status is not (InvoiceStatus.Void or InvoiceStatus.Draft or InvoiceStatus.WrittenOff))
            .Where(i => i.Balance > 0.005m)
            .ToList();

        decimal current = 0, b30 = 0, b60 = 0, b90 = 0, over = 0;
        int cCount = 0, c30 = 0, c60 = 0, c90 = 0, cOver = 0;

        foreach (var invoice in outstanding)
        {
            var days = today.DayNumber - invoice.DueDate.DayNumber;
            switch (days)
            {
                case <= 0: current += invoice.Balance; cCount++; break;
                case <= 30: b30 += invoice.Balance; c30++; break;
                case <= 60: b60 += invoice.Balance; c60++; break;
                case <= 90: b90 += invoice.Balance; c90++; break;
                default: over += invoice.Balance; cOver++; break;
            }
        }

        return new AgingReport
        {
            Current = Math.Round(current, 2),
            Days1To30 = Math.Round(b30, 2),
            Days31To60 = Math.Round(b60, 2),
            Days61To90 = Math.Round(b90, 2),
            Over90Days = Math.Round(over, 2),
            Buckets = new[]
            {
                new AgingBucket("Not yet due", Math.Round(current, 2), cCount),
                new AgingBucket("1-30 days", Math.Round(b30, 2), c30),
                new AgingBucket("31-60 days", Math.Round(b60, 2), c60),
                new AgingBucket("61-90 days", Math.Round(b90, 2), c90),
                new AgingBucket("Over 90 days", Math.Round(over, 2), cOver)
            }
        };
    }

    /// <summary>
    /// Allocates a payment across invoices oldest-first, returning the amount
    /// applied to each and any unallocated remainder.
    /// </summary>
    public (List<(Invoice Invoice, decimal Amount)> Allocations, decimal Unallocated) AllocateOldestFirst(
        decimal paymentAmount, IEnumerable<Invoice> invoices)
    {
        var allocations = new List<(Invoice, decimal)>();
        var remaining = paymentAmount;

        var candidates = invoices
            .Where(i => i.Balance > 0.005m &&
                        i.Status is not (InvoiceStatus.Void or InvoiceStatus.Draft or InvoiceStatus.WrittenOff))
            .OrderBy(i => i.DueDate)
            .ThenBy(i => i.IssueDate);

        foreach (var invoice in candidates)
        {
            if (remaining <= 0.005m) break;
            var applied = Math.Min(remaining, invoice.Balance);
            allocations.Add((invoice, Math.Round(applied, 2)));
            remaining -= applied;
        }

        return (allocations, Math.Round(Math.Max(0m, remaining), 2));
    }

    /// <summary>Builds the installment schedule for a payment plan.</summary>
    public List<PaymentPlanInstallment> BuildInstallments(PaymentPlan plan)
    {
        var list = new List<PaymentPlanInstallment>();
        if (plan.NumberOfInstallments <= 0) return list;

        var financed = plan.TotalAmount - plan.DownPayment;
        var withInterest = financed * (1 + plan.InterestRatePercent / 100m) + plan.AdministrationFee;
        var each = Math.Round(withInterest / plan.NumberOfInstallments, 2);
        var allocated = 0m;

        var due = plan.StartDate;
        for (var i = 1; i <= plan.NumberOfInstallments; i++)
        {
            due = i == 1 ? due : Advance(due, plan.Frequency);

            // The last installment absorbs the rounding remainder.
            var amount = i == plan.NumberOfInstallments
                ? Math.Round(withInterest - allocated, 2)
                : each;
            allocated += amount;

            list.Add(new PaymentPlanInstallment
            {
                PaymentPlanId = plan.Id,
                InstallmentNumber = i,
                DueDate = due,
                AmountDue = amount,
                Status = InstallmentStatus.Scheduled
            });
        }

        return list;
    }

    private static DateOnly Advance(DateOnly date, PaymentFrequency frequency) => frequency switch
    {
        PaymentFrequency.Weekly => date.AddDays(7),
        PaymentFrequency.Fortnightly => date.AddDays(14),
        PaymentFrequency.Quarterly => date.AddMonths(3),
        _ => date.AddMonths(1)
    };
}
