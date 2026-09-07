using DentalSurgery.Application.Billing;
using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;
using Xunit;

namespace DentalSurgery.Tests;

public class InsuranceEstimatorTests
{
    private readonly InsuranceEstimator _estimator = new();

    private static ProcedureCode Code(ProcedureCategory category, decimal fee = 500m) => new()
    {
        Code = "D0000", ShortDescription = "Test procedure", Category = category, DefaultFee = fee
    };

    private static PatientInsurance Policy(
        decimal basicPercent = 80m, decimal? annualMax = null, decimal? deductible = null,
        decimal used = 0m, decimal deductibleMet = 0m, int? waitingMonths = null,
        int enrolledMonthsAgo = 24) => new()
    {
        IsActive = true,
        EffectiveFrom = DateOnly.FromDateTime(DateTime.Today.AddMonths(-enrolledMonthsAgo)),
        BenefitsUsedThisYear = used,
        DeductibleMetThisYear = deductibleMet,
        InsurancePlan = new InsurancePlan
        {
            PlanName = "Test plan",
            BasicCoveragePercent = basicPercent,
            AnnualMaximum = annualMax,
            IndividualDeductible = deductible,
            WaitingPeriodBasicMonths = waitingMonths
        }
    };

    [Fact]
    public void No_policy_means_the_patient_pays_in_full()
    {
        var estimate = _estimator.Estimate(Code(ProcedureCategory.Restorative), 200m, null);

        Assert.Equal(0m, estimate.InsurancePortion);
        Assert.Equal(200m, estimate.PatientPortion);
    }

    [Fact]
    public void Coverage_is_applied_at_the_category_percentage()
    {
        var estimate = _estimator.Estimate(Code(ProcedureCategory.Restorative), 200m, Policy(basicPercent: 80m));

        Assert.Equal(160m, estimate.InsurancePortion);
        Assert.Equal(40m, estimate.PatientPortion);
    }

    [Fact]
    public void The_deductible_is_taken_before_coverage_is_calculated()
    {
        // 200 fee, 50 deductible outstanding: the insurer covers 80% of the remaining 150.
        var estimate = _estimator.Estimate(
            Code(ProcedureCategory.Restorative), 200m, Policy(basicPercent: 80m, deductible: 50m));

        Assert.Equal(50m, estimate.DeductibleApplied);
        Assert.Equal(120m, estimate.InsurancePortion);
        Assert.Equal(80m, estimate.PatientPortion);
    }

    [Fact]
    public void A_partly_met_deductible_only_takes_the_remainder()
    {
        var estimate = _estimator.Estimate(
            Code(ProcedureCategory.Restorative), 200m,
            Policy(basicPercent: 80m, deductible: 50m, deductibleMet: 30m));

        Assert.Equal(20m, estimate.DeductibleApplied);
        Assert.Equal(144m, estimate.InsurancePortion);
    }

    [Fact]
    public void Benefit_is_capped_at_the_remaining_annual_maximum()
    {
        // 1000 maximum with 900 already used leaves 100 of benefit.
        var estimate = _estimator.Estimate(
            Code(ProcedureCategory.Restorative), 500m, Policy(annualMax: 1000m, used: 900m));

        Assert.Equal(100m, estimate.InsurancePortion);
        Assert.Equal(400m, estimate.PatientPortion);
        Assert.Contains("Capped", estimate.Explanation);
    }

    [Fact]
    public void An_exhausted_annual_maximum_pays_nothing()
    {
        var estimate = _estimator.Estimate(
            Code(ProcedureCategory.Restorative), 500m, Policy(annualMax: 1000m, used: 1000m));

        Assert.Equal(0m, estimate.InsurancePortion);
        Assert.Equal(500m, estimate.PatientPortion);
    }

    [Fact]
    public void Treatment_inside_a_waiting_period_is_not_covered()
    {
        var estimate = _estimator.Estimate(
            Code(ProcedureCategory.Restorative), 300m,
            Policy(waitingMonths: 6, enrolledMonthsAgo: 2));

        Assert.Equal(0m, estimate.InsurancePortion);
        Assert.Contains("waiting period", estimate.Explanation);
    }

    [Fact]
    public void Preventive_treatment_skips_the_deductible_by_default()
    {
        var estimate = _estimator.Estimate(
            Code(ProcedureCategory.Preventive), 80m, Policy(deductible: 50m));

        Assert.Equal(0m, estimate.DeductibleApplied);
        Assert.Equal(80m, estimate.InsurancePortion);
    }

    [Fact]
    public void Secondary_cover_picks_up_part_of_what_the_primary_leaves()
    {
        var primary = Policy(basicPercent: 60m);
        primary.Priority = InsurancePriority.Primary;

        var secondary = Policy(basicPercent: 50m);
        secondary.Priority = InsurancePriority.Secondary;

        // Primary pays 60% of 100 = 60. Secondary pays 50% of the remaining 40 = 20.
        var estimate = _estimator.EstimateCoordinated(
            Code(ProcedureCategory.Restorative), 100m, new[] { primary, secondary });

        Assert.Equal(80m, estimate.InsurancePortion);
        Assert.Equal(20m, estimate.PatientPortion);
    }

    [Fact]
    public void Coordinated_benefit_never_exceeds_the_fee()
    {
        var primary = Policy(basicPercent: 100m);
        primary.Priority = InsurancePriority.Primary;

        var secondary = Policy(basicPercent: 100m);
        secondary.Priority = InsurancePriority.Secondary;

        var estimate = _estimator.EstimateCoordinated(
            Code(ProcedureCategory.Restorative), 100m, new[] { primary, secondary });

        Assert.Equal(100m, estimate.InsurancePortion);
        Assert.Equal(0m, estimate.PatientPortion);
    }

    [Fact]
    public void A_terminated_policy_is_ignored()
    {
        var policy = Policy();
        policy.TerminatedOn = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));

        var estimate = _estimator.Estimate(Code(ProcedureCategory.Restorative), 200m, policy);

        Assert.Equal(0m, estimate.InsurancePortion);
    }
}

public class LedgerCalculatorTests
{
    private readonly LedgerCalculator _calculator = new();

    private static Invoice Invoice(decimal total, decimal paid, int dueDaysAgo) => new()
    {
        InvoiceNumber = $"INV-{Guid.NewGuid():N}"[..12],
        Total = total,
        AmountPaid = paid,
        DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-dueDaysAgo)),
        IssueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-dueDaysAgo - 30)),
        Status = InvoiceStatus.Issued
    };

    [Fact]
    public void Running_balances_accumulate_in_date_order()
    {
        var entries = new List<LedgerEntry>
        {
            new() { EntryDate = new DateOnly(2026, 1, 10), Debit = 100m, CreatedAtUtc = new DateTime(2026, 1, 10) },
            new() { EntryDate = new DateOnly(2026, 1, 15), Credit = 40m, CreatedAtUtc = new DateTime(2026, 1, 15) },
            new() { EntryDate = new DateOnly(2026, 1, 20), Debit = 25m, CreatedAtUtc = new DateTime(2026, 1, 20) }
        };

        var closing = _calculator.Recalculate(entries);

        Assert.Equal(85m, closing);
        Assert.Equal(100m, entries[0].RunningBalance);
        Assert.Equal(60m, entries[1].RunningBalance);
        Assert.Equal(85m, entries[2].RunningBalance);
    }

    [Fact]
    public void Ageing_places_each_invoice_in_the_right_bucket()
    {
        var invoices = new List<Invoice>
        {
            Invoice(100m, 0m, -5),   // due in five days
            Invoice(200m, 0m, 15),
            Invoice(300m, 0m, 45),
            Invoice(400m, 0m, 75),
            Invoice(500m, 0m, 120)
        };

        var report = _calculator.BuildAging(invoices);

        Assert.Equal(100m, report.Current);
        Assert.Equal(200m, report.Days1To30);
        Assert.Equal(300m, report.Days31To60);
        Assert.Equal(400m, report.Days61To90);
        Assert.Equal(500m, report.Over90Days);
        Assert.Equal(1500m, report.Total);
    }

    [Fact]
    public void Ageing_ignores_settled_and_void_invoices()
    {
        var paid = Invoice(100m, 100m, 40);
        var voided = Invoice(200m, 0m, 40);
        voided.Status = InvoiceStatus.Void;

        var report = _calculator.BuildAging(new[] { paid, voided });

        Assert.Equal(0m, report.Total);
    }

    [Fact]
    public void Payments_are_allocated_to_the_oldest_invoice_first()
    {
        var oldest = Invoice(100m, 0m, 60);
        var newest = Invoice(100m, 0m, 10);

        var (allocations, unallocated) = _calculator.AllocateOldestFirst(150m, new[] { newest, oldest });

        Assert.Equal(2, allocations.Count);
        Assert.Same(oldest, allocations[0].Invoice);
        Assert.Equal(100m, allocations[0].Amount);
        Assert.Equal(50m, allocations[1].Amount);
        Assert.Equal(0m, unallocated);
    }

    [Fact]
    public void An_overpayment_leaves_an_unallocated_remainder()
    {
        var invoice = Invoice(80m, 0m, 20);

        var (allocations, unallocated) = _calculator.AllocateOldestFirst(120m, new[] { invoice });

        Assert.Single(allocations);
        Assert.Equal(80m, allocations[0].Amount);
        Assert.Equal(40m, unallocated);
    }

    [Fact]
    public void Instalments_sum_exactly_to_the_financed_amount()
    {
        var plan = new PaymentPlan
        {
            TotalAmount = 1000m,
            DownPayment = 100m,
            NumberOfInstallments = 7,
            Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2026, 1, 15)
        };

        var instalments = _calculator.BuildInstallments(plan);

        Assert.Equal(7, instalments.Count);
        // 900 over 7 does not divide evenly; the final instalment absorbs the rounding.
        Assert.Equal(900m, instalments.Sum(i => i.AmountDue));
        Assert.Equal(new DateOnly(2026, 1, 15), instalments[0].DueDate);
        Assert.Equal(new DateOnly(2026, 7, 15), instalments[^1].DueDate);
    }

    [Fact]
    public void Interest_and_the_administration_fee_are_spread_across_the_instalments()
    {
        var plan = new PaymentPlan
        {
            TotalAmount = 1000m,
            DownPayment = 0m,
            NumberOfInstallments = 10,
            InterestRatePercent = 10m,
            AdministrationFee = 50m,
            Frequency = PaymentFrequency.Monthly,
            StartDate = new DateOnly(2026, 3, 1)
        };

        var instalments = _calculator.BuildInstallments(plan);

        Assert.Equal(1150m, instalments.Sum(i => i.AmountDue));
    }

    [Fact]
    public void Weekly_plans_advance_by_seven_days()
    {
        var plan = new PaymentPlan
        {
            TotalAmount = 400m,
            NumberOfInstallments = 4,
            Frequency = PaymentFrequency.Weekly,
            StartDate = new DateOnly(2026, 2, 2)
        };

        var instalments = _calculator.BuildInstallments(plan);

        Assert.Equal(new DateOnly(2026, 2, 2), instalments[0].DueDate);
        Assert.Equal(new DateOnly(2026, 2, 23), instalments[3].DueDate);
    }
}

public class FeeCalculatorTests
{
    private readonly FeeCalculator _calculator = new();

    [Fact]
    public void Without_a_schedule_the_catalogue_fee_applies()
    {
        var code = new ProcedureCode { Code = "D2140", DefaultFee = 95m };
        Assert.Equal(95m, _calculator.ResolveFee(code, null));
    }

    [Fact]
    public void An_explicit_schedule_price_overrides_the_catalogue()
    {
        var code = new ProcedureCode { Code = "D2140", DefaultFee = 95m };
        var schedule = new FeeSchedule { Name = "Insurer" };
        var items = new[] { new FeeScheduleItem { FeeScheduleId = schedule.Id, ProcedureCodeId = code.Id, Fee = 72m } };

        Assert.Equal(72m, _calculator.ResolveFee(code, schedule, items));
    }

    [Fact]
    public void A_blanket_adjustment_applies_when_no_explicit_price_exists()
    {
        var code = new ProcedureCode { Code = "D2140", DefaultFee = 100m };
        var schedule = new FeeSchedule { Name = "Membership", BlanketAdjustmentPercent = -15m };

        Assert.Equal(85m, _calculator.ResolveFee(code, schedule));
    }

    [Fact]
    public void Line_totals_apply_quantity_then_discount_then_tax()
    {
        // 2 x 100 = 200, less 20 discount = 180, plus 10% tax = 198.
        Assert.Equal(198m, _calculator.LineTotal(100m, 2m, 20m, 10m));
    }

    [Fact]
    public void A_discount_larger_than_the_line_cannot_make_it_negative()
    {
        Assert.Equal(0m, _calculator.LineTotal(50m, 1m, 80m, 0m));
    }
}
