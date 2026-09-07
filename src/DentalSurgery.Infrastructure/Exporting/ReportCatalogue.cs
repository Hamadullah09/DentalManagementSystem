using DentalSurgery.Application.Abstractions;
using DentalSurgery.Application.Exporting;
using DentalSurgery.Domain.Enums;
using DentalSurgery.Infrastructure.Persistence;
using DentalSurgery.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace DentalSurgery.Infrastructure.Exporting;

/// <summary>
/// Every report the practice can export, defined once and rendered to any
/// format. Each builder returns a <see cref="ReportTable"/>, so adding a report
/// means adding one query rather than four exporters.
/// </summary>
public class ReportCatalogue(
    IDbContextFactory<DentalDbContext> dbFactory,
    ReportingService reporting,
    ICurrentUser currentUser,
    IDateTimeProvider clock)
{
    public static readonly IReadOnlyList<ReportDefinition> Definitions = new List<ReportDefinition>
    {
        new("production-by-category", "Production by category", "Fee income grouped by treatment category.", "Finance"),
        new("daily-production", "Daily production and collections", "Day-by-day production against money collected.", "Finance"),
        new("receivables", "Outstanding balances", "Every account in debit, aged from the invoice due date.", "Finance", false),
        new("invoices", "Invoices", "Invoices raised in the period with their settlement status.", "Finance"),
        new("payments", "Payments received", "Every payment taken, by method.", "Finance"),
        new("claims", "Insurance claims", "Claims and pre-authorisations with what each payer settled.", "Finance"),

        new("appointments", "Appointment book", "Every booking with its outcome, wait and chair time.", "Scheduling"),
        new("provider-utilisation", "Provider utilisation", "Booked minutes against rostered minutes per clinician.", "Scheduling"),
        new("recalls", "Recall worklist", "Patients due or overdue for review.", "Scheduling", false),
        new("waitlist", "Waiting list", "Patients wanting an earlier appointment.", "Scheduling", false),

        new("procedures", "Procedures completed", "Every completed procedure with tooth, provider and fee.", "Clinical"),
        new("surgical-register", "Surgical register", "Operative procedures with outcomes and complications.", "Clinical"),
        new("implants", "Implant registry", "Every fixture placed, traceable by lot number.", "Clinical", false),
        new("prescriptions", "Prescriptions issued", "Prescribing activity, including controlled drugs.", "Clinical"),
        new("radiographs", "Imaging and dose", "Radiographic exposures with justification and dose.", "Clinical"),
        new("treatment-acceptance", "Treatment plan acceptance", "Plans presented against value accepted.", "Clinical"),
        new("patients", "Patient register", "The patient list with recall and balance.", "Clinical", false),

        new("stock", "Stock levels", "Current stock against reorder levels and value.", "Operations", false),
        new("expiring-stock", "Expiring stock", "Lots at or near their expiry date.", "Operations", false),
        new("sterilisation", "Sterilisation log", "Cycle records with indicator results.", "Operations"),
        new("lab-cases", "Laboratory cases", "Work sent out, due dates and remakes.", "Operations", false),
        new("audit", "Audit trail", "Record changes with the user who made them.", "Governance")
    };

    public static ReportDefinition? Find(string key) =>
        Definitions.FirstOrDefault(d => d.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

    public async Task<ReportTable?> BuildAsync(string key, DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        var definition = Find(key);
        if (definition is null) return null;

        var end = to ?? clock.Today;
        var start = from ?? new DateOnly(end.Year, end.Month, 1);

        return key.ToLowerInvariant() switch
        {
            "production-by-category" => await ProductionByCategoryAsync(start, end, ct),
            "daily-production" => await DailyProductionAsync(start, end, ct),
            "receivables" => await ReceivablesAsync(ct),
            "invoices" => await InvoicesAsync(start, end, ct),
            "payments" => await PaymentsAsync(start, end, ct),
            "claims" => await ClaimsAsync(start, end, ct),
            "appointments" => await AppointmentsAsync(start, end, ct),
            "provider-utilisation" => await ProviderUtilisationAsync(start, end, ct),
            "recalls" => await RecallsAsync(ct),
            "waitlist" => await WaitlistAsync(ct),
            "procedures" => await ProceduresAsync(start, end, ct),
            "surgical-register" => await SurgicalRegisterAsync(start, end, ct),
            "implants" => await ImplantsAsync(ct),
            "prescriptions" => await PrescriptionsAsync(start, end, ct),
            "radiographs" => await RadiographsAsync(start, end, ct),
            "treatment-acceptance" => await TreatmentAcceptanceAsync(start, end, ct),
            "patients" => await PatientsAsync(ct),
            "stock" => await StockAsync(ct),
            "expiring-stock" => await ExpiringStockAsync(ct),
            "sterilisation" => await SterilisationAsync(start, end, ct),
            "lab-cases" => await LabCasesAsync(ct),
            "audit" => await AuditAsync(start, end, ct),
            _ => null
        };
    }

    // ---------------------------------------------------------------- finance

    private async Task<ReportTable> ProductionByCategoryAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var summary = await reporting.GetFinancialSummaryAsync(from, to, ct);

        return Table("production-by-category", "Production by category",
            "Completed treatment valued at the fee charged.", from, to,
            new[]
            {
                new ReportColumn("Category", ReportColumnType.Text, Width: 2.5),
                new ReportColumn("Procedures", ReportColumnType.Integer, Total: true),
                new ReportColumn("Value", ReportColumnType.Money, Total: true),
                new ReportColumn("Share", ReportColumnType.Percent)
            },
            summary.ByCategory.Select(c => new object?[]
            {
                c.Label, c.Count, c.Amount, c.PercentOf(summary.GrossProduction)
            }).ToList(),
            new[]
            {
                new ReportMetric("Gross production", Money(summary.GrossProduction)),
                new ReportMetric("Net production", Money(summary.NetProduction), "after discounts and write-offs"),
                new ReportMetric("Collections", Money(summary.TotalCollections),
                    $"{summary.CollectionRatePercent:0.#}% of net production")
            });
    }

    private async Task<ReportTable> DailyProductionAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var summary = await reporting.GetFinancialSummaryAsync(from, to, ct);

        var collections = summary.DailyCollections.ToDictionary(d => d.Date, d => d.Value);

        var rows = summary.DailyProduction.Select(day => new object?[]
        {
            day.Date,
            day.Date.DayOfWeek.ToString(),
            day.Count,
            day.Value,
            collections.GetValueOrDefault(day.Date),
            collections.GetValueOrDefault(day.Date) - day.Value
        }).ToList();

        return Table("daily-production", "Daily production and collections",
            "Production is the value of treatment completed; collections is money received.", from, to,
            new[]
            {
                new ReportColumn("Date", ReportColumnType.Date),
                new ReportColumn("Day", ReportColumnType.Text),
                new ReportColumn("Procedures", ReportColumnType.Integer, Total: true),
                new ReportColumn("Production", ReportColumnType.Money, Total: true),
                new ReportColumn("Collections", ReportColumnType.Money, Total: true),
                new ReportColumn("Variance", ReportColumnType.Money, Total: true)
            },
            rows,
            new[]
            {
                new ReportMetric("Production", Money(summary.GrossProduction)),
                new ReportMetric("Collections", Money(summary.TotalCollections)),
                new ReportMetric("Collection rate", $"{summary.CollectionRatePercent:0.#}%")
            });
    }

    private async Task<ReportTable> ReceivablesAsync(CancellationToken ct)
    {
        var aging = await reporting.GetAgingAsync(ct);
        var rows = await reporting.GetReceivablesAsync(ct);

        return Table("receivables", "Outstanding balances",
            "Aged from each invoice due date.", null, null,
            new[]
            {
                new ReportColumn("Patient number", ReportColumnType.Text),
                new ReportColumn("Patient", ReportColumnType.Text, Width: 2),
                new ReportColumn("Phone", ReportColumnType.Text),
                new ReportColumn("Current", ReportColumnType.Money, Total: true),
                new ReportColumn("1-30", ReportColumnType.Money, Total: true),
                new ReportColumn("31-60", ReportColumnType.Money, Total: true),
                new ReportColumn("61-90", ReportColumnType.Money, Total: true),
                new ReportColumn("Over 90", ReportColumnType.Money, Total: true),
                new ReportColumn("Balance", ReportColumnType.Money, Total: true),
                new ReportColumn("Oldest due", ReportColumnType.Date),
                new ReportColumn("Last payment", ReportColumnType.Date)
            },
            rows.Select(r => new object?[]
            {
                r.PatientNumber, r.PatientName, r.Phone,
                r.Current, r.Days1To30, r.Days31To60, r.Days61To90, r.Over90, r.Balance,
                r.OldestDueDate, r.LastPaymentDate
            }).ToList(),
            new[]
            {
                new ReportMetric("Total outstanding", Money(aging.Total)),
                new ReportMetric("Over 90 days", Money(aging.Over90Days), $"{aging.PercentOver90:0.#}% of the total"),
                new ReportMetric("Accounts in debit", rows.Count.ToString("N0"))
            });
    }

    private async Task<ReportTable> InvoicesAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var invoices = await db.Invoices.AsNoTracking()
            .Include(i => i.Patient).Include(i => i.Provider)
            .Where(i => i.IssueDate >= from && i.IssueDate <= to)
            .OrderByDescending(i => i.IssueDate)
            .ToListAsync(ct);

        return Table("invoices", "Invoices", "Invoices raised in the period.", from, to,
            new[]
            {
                new ReportColumn("Invoice", ReportColumnType.Text),
                new ReportColumn("Issued", ReportColumnType.Date),
                new ReportColumn("Due", ReportColumnType.Date),
                new ReportColumn("Patient", ReportColumnType.Text, Width: 2),
                new ReportColumn("Provider", ReportColumnType.Text, Width: 1.5),
                new ReportColumn("Total", ReportColumnType.Money, Total: true),
                new ReportColumn("Paid", ReportColumnType.Money, Total: true),
                new ReportColumn("Balance", ReportColumnType.Money, Total: true),
                new ReportColumn("Status", ReportColumnType.Text)
            },
            invoices.Select(i => new object?[]
            {
                i.InvoiceNumber, i.IssueDate, i.DueDate,
                i.Patient?.Name.Display, i.Provider?.DisplayName,
                i.Total, i.AmountPaid, i.Balance, i.Status
            }).ToList(),
            new[]
            {
                new ReportMetric("Invoiced", Money(invoices.Sum(i => i.Total))),
                new ReportMetric("Collected", Money(invoices.Sum(i => i.AmountPaid))),
                new ReportMetric("Outstanding", Money(invoices.Sum(i => i.Balance)),
                    $"{invoices.Count(i => i.IsOverdue)} overdue")
            });
    }

    private async Task<ReportTable> PaymentsAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var payments = await db.Payments.AsNoTracking()
            .Include(p => p.Patient)
            .Where(p => p.PaymentDate >= from && p.PaymentDate <= to)
            .OrderByDescending(p => p.PaymentDate)
            .ToListAsync(ct);

        var byMethod = payments.GroupBy(p => p.Method)
            .Select(g => new ReportMetric(CsvReportExporter.Humanise(g.Key.ToString()),
                Money(g.Sum(p => p.Amount - p.RefundedAmount)), $"{g.Count()} payments"))
            .OrderByDescending(m => m.Value)
            .Take(3)
            .ToList();

        return Table("payments", "Payments received", "Every payment taken in the period.", from, to,
            new[]
            {
                new ReportColumn("Payment", ReportColumnType.Text),
                new ReportColumn("Date", ReportColumnType.Date),
                new ReportColumn("Patient", ReportColumnType.Text, Width: 2),
                new ReportColumn("Method", ReportColumnType.Text),
                new ReportColumn("Reference", ReportColumnType.Text, Width: 1.5),
                new ReportColumn("Amount", ReportColumnType.Money, Total: true),
                new ReportColumn("Refunded", ReportColumnType.Money, Total: true),
                new ReportColumn("Status", ReportColumnType.Text)
            },
            payments.Select(p => new object?[]
            {
                p.PaymentNumber, p.PaymentDate, p.Patient?.Name.Display, p.Method,
                p.ReferenceNumber, p.Amount, p.RefundedAmount, p.Status
            }).ToList(),
            byMethod);
    }

    private async Task<ReportTable> ClaimsAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var claims = await db.InsuranceClaims.AsNoTracking()
            .Include(c => c.Patient)
            .Include(c => c.PatientInsurance).ThenInclude(i => i!.InsurancePlan).ThenInclude(p => p!.InsuranceCarrier)
            .Where(c => c.ServiceDate >= from && c.ServiceDate <= to)
            .OrderByDescending(c => c.ServiceDate)
            .ToListAsync(ct);

        return Table("claims", "Insurance claims", "Claims and pre-authorisations.", from, to,
            new[]
            {
                new ReportColumn("Claim", ReportColumnType.Text),
                new ReportColumn("Patient", ReportColumnType.Text, Width: 2),
                new ReportColumn("Carrier", ReportColumnType.Text, Width: 1.8),
                new ReportColumn("Service date", ReportColumnType.Date),
                new ReportColumn("Submitted", ReportColumnType.Date),
                new ReportColumn("Charged", ReportColumnType.Money, Total: true),
                new ReportColumn("Paid", ReportColumnType.Money, Total: true),
                new ReportColumn("Patient owes", ReportColumnType.Money, Total: true),
                new ReportColumn("Days open", ReportColumnType.Integer),
                new ReportColumn("Status", ReportColumnType.Text)
            },
            claims.Select(c => new object?[]
            {
                c.ClaimNumber, c.Patient?.Name.Display,
                c.PatientInsurance?.InsurancePlan?.InsuranceCarrier?.Name,
                c.ServiceDate, c.SubmittedOn,
                c.TotalCharged, c.TotalPaid, c.PatientResponsibility,
                c.DaysOutstanding, c.Status
            }).ToList(),
            new[]
            {
                new ReportMetric("Charged", Money(claims.Sum(c => c.TotalCharged))),
                new ReportMetric("Received", Money(claims.Sum(c => c.TotalPaid))),
                new ReportMetric("Awaiting settlement",
                    Money(claims.Where(c => c.Status is not (ClaimStatus.Paid or ClaimStatus.Closed or ClaimStatus.Denied))
                                .Sum(c => c.TotalCharged - c.TotalPaid)))
            });
    }

    // ---------------------------------------------------------------- scheduling

    private async Task<ReportTable> AppointmentsAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var start = from.ToDateTime(TimeOnly.MinValue);
        var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var appointments = await db.Appointments.AsNoTracking()
            .Include(a => a.Patient).Include(a => a.Provider).Include(a => a.Operatory)
            .Where(a => a.StartUtc >= start && a.StartUtc < end)
            .OrderBy(a => a.StartUtc)
            .ToListAsync(ct);

        var analytics = await reporting.GetScheduleAnalyticsAsync(from, to, ct);

        return Table("appointments", "Appointment book", "Every booking in the period.", from, to,
            new[]
            {
                new ReportColumn("Date", ReportColumnType.Date),
                new ReportColumn("Time", ReportColumnType.Text),
                new ReportColumn("Minutes", ReportColumnType.Integer, Total: true),
                new ReportColumn("Patient", ReportColumnType.Text, Width: 2),
                new ReportColumn("Provider", ReportColumnType.Text, Width: 1.5),
                new ReportColumn("Surgery", ReportColumnType.Text),
                new ReportColumn("Type", ReportColumnType.Text),
                new ReportColumn("Wait", ReportColumnType.Integer),
                new ReportColumn("Chair", ReportColumnType.Integer),
                new ReportColumn("Status", ReportColumnType.Text)
            },
            appointments.Select(a => new object?[]
            {
                DateOnly.FromDateTime(a.StartUtc), a.StartUtc.ToString("HH:mm"), a.DurationMinutes,
                a.Patient?.Name.Display, a.Provider?.DisplayName, a.Operatory?.Name,
                a.AppointmentType, a.WaitingMinutes, a.ChairMinutes, a.Status
            }).ToList(),
            new[]
            {
                new ReportMetric("Appointments", analytics.TotalAppointments.ToString("N0")),
                new ReportMetric("No-show rate", $"{analytics.NoShowRatePercent:0.#}%",
                    $"{analytics.NoShows} missed"),
                new ReportMetric("Utilisation", $"{analytics.UtilisationPercent:0.#}%",
                    $"{analytics.TotalBookedMinutes / 60:N0} of {analytics.TotalAvailableMinutes / 60:N0} hours")
            });
    }

    private async Task<ReportTable> ProviderUtilisationAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var analytics = await reporting.GetScheduleAnalyticsAsync(from, to, ct);

        return Table("provider-utilisation", "Provider utilisation",
            "Booked minutes against rostered minutes.", from, to,
            new[]
            {
                new ReportColumn("Provider", ReportColumnType.Text, Width: 2.5),
                new ReportColumn("Appointments", ReportColumnType.Integer, Total: true),
                new ReportColumn("Utilisation", ReportColumnType.Percent)
            },
            analytics.ByProvider.Select(p => new object?[]
            {
                p.ProviderName, p.Appointments, p.UtilisationPercent
            }).ToList(),
            new[]
            {
                new ReportMetric("Overall utilisation", $"{analytics.UtilisationPercent:0.#}%"),
                new ReportMetric("Average wait", $"{analytics.AverageWaitMinutes:0} min"),
                new ReportMetric("Average chair time", $"{analytics.AverageChairMinutes:0} min")
            });
    }

    private async Task<ReportTable> RecallsAsync(CancellationToken ct)
    {
        var recalls = await reporting.GetRecallsDueAsync(clock.Today.AddDays(60), false, ct);

        return Table("recalls", "Recall worklist", "Patients due or overdue for review.", null, null,
            new[]
            {
                new ReportColumn("Patient number", ReportColumnType.Text),
                new ReportColumn("Patient", ReportColumnType.Text, Width: 2),
                new ReportColumn("Type", ReportColumnType.Text),
                new ReportColumn("Due", ReportColumnType.Date),
                new ReportColumn("Days overdue", ReportColumnType.Integer),
                new ReportColumn("Phone", ReportColumnType.Text),
                new ReportColumn("Email", ReportColumnType.Text, Width: 2),
                new ReportColumn("Provider", ReportColumnType.Text, Width: 1.5),
                new ReportColumn("Attempts", ReportColumnType.Integer)
            },
            recalls.Select(r => new object?[]
            {
                r.PatientNumber, r.PatientName, r.RecallType, r.DueDate, r.DaysOverdue,
                r.Phone, r.Email, r.ProviderName, r.ContactAttempts
            }).ToList(),
            new[]
            {
                new ReportMetric("Due", recalls.Count.ToString("N0")),
                new ReportMetric("Overdue", recalls.Count(r => r.DaysOverdue > 0).ToString("N0")),
                new ReportMetric("Over six months late", recalls.Count(r => r.DaysOverdue > 180).ToString("N0"))
            });
    }

    private async Task<ReportTable> WaitlistAsync(CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var entries = await db.WaitlistEntries.AsNoTracking()
            .Include(w => w.Patient).Include(w => w.PreferredProvider)
            .Where(w => w.Status != WaitlistStatus.Cancelled && w.Status != WaitlistStatus.Expired)
            .OrderByDescending(w => w.Priority)
            .ToListAsync(ct);

        return Table("waitlist", "Waiting list", "Patients wanting an earlier appointment.", null, null,
            new[]
            {
                new ReportColumn("Patient", ReportColumnType.Text, Width: 2),
                new ReportColumn("Phone", ReportColumnType.Text),
                new ReportColumn("Wanted for", ReportColumnType.Text, Width: 1.5),
                new ReportColumn("Minutes", ReportColumnType.Integer),
                new ReportColumn("Provider", ReportColumnType.Text, Width: 1.5),
                new ReportColumn("Available from", ReportColumnType.Date),
                new ReportColumn("Priority", ReportColumnType.Text),
                new ReportColumn("Attempts", ReportColumnType.Integer),
                new ReportColumn("Status", ReportColumnType.Text)
            },
            entries.Select(w => new object?[]
            {
                w.Patient?.Name.Display, w.Patient?.Contact.BestPhone, w.AppointmentType,
                w.EstimatedMinutes, w.PreferredProvider?.DisplayName, w.AvailableFrom,
                w.Priority, w.ContactAttempts, w.Status
            }).ToList(),
            new[]
            {
                new ReportMetric("Waiting", entries.Count(w => w.Status == WaitlistStatus.Waiting).ToString()),
                new ReportMetric("Urgent", entries.Count(w => w.Priority >= WaitlistPriority.High).ToString())
            });
    }

    // ---------------------------------------------------------------- clinical

    private async Task<ReportTable> ProceduresAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var start = from.ToDateTime(TimeOnly.MinValue);
        var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var procedures = await db.Procedures.AsNoTracking()
            .Include(p => p.Patient).Include(p => p.ProcedureCode)
            .Include(p => p.Tooth).Include(p => p.Provider)
            .Where(p => p.DateOfService >= start && p.DateOfService < end && p.Status == ProcedureStatus.Completed)
            .OrderByDescending(p => p.DateOfService)
            .ToListAsync(ct);

        return Table("procedures", "Procedures completed", "Completed treatment in the period.", from, to,
            new[]
            {
                new ReportColumn("Date", ReportColumnType.Date),
                new ReportColumn("Patient", ReportColumnType.Text, Width: 2),
                new ReportColumn("Code", ReportColumnType.Text),
                new ReportColumn("Procedure", ReportColumnType.Text, Width: 2.5),
                new ReportColumn("Category", ReportColumnType.Text, Width: 1.5),
                new ReportColumn("Tooth", ReportColumnType.Text),
                new ReportColumn("Surfaces", ReportColumnType.Text),
                new ReportColumn("Provider", ReportColumnType.Text, Width: 1.5),
                new ReportColumn("Fee", ReportColumnType.Money, Total: true)
            },
            procedures.Select(p => new object?[]
            {
                DateOnly.FromDateTime(p.DateOfService), p.Patient?.Name.Display,
                p.ProcedureCode?.Code, p.ProcedureCode?.ShortDescription,
                p.ProcedureCode?.Category, p.Tooth?.FdiNumber,
                p.Surfaces == ToothSurface.None ? null : Domain.Entities.SurfaceNotation.ToCode(p.Surfaces),
                p.Provider?.DisplayName, p.NetFee
            }).ToList(),
            new[]
            {
                new ReportMetric("Procedures", procedures.Count.ToString("N0")),
                new ReportMetric("Value", Money(procedures.Sum(p => p.NetFee))),
                new ReportMetric("Average fee",
                    Money(procedures.Count == 0 ? 0 : procedures.Sum(p => p.NetFee) / procedures.Count))
            });
    }

    private async Task<ReportTable> SurgicalRegisterAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var start = from.ToDateTime(TimeOnly.MinValue);
        var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var cases = await db.Procedures.AsNoTracking()
            .Include(p => p.Patient).Include(p => p.ProcedureCode)
            .Include(p => p.Tooth).Include(p => p.Provider).Include(p => p.SurgicalRecord)
            .Where(p => p.SurgicalRecord != null && p.DateOfService >= start && p.DateOfService < end &&
                        p.Status != ProcedureStatus.Voided)
            .OrderByDescending(p => p.DateOfService)
            .ToListAsync(ct);

        var complications = cases.Count(c => c.SurgicalRecord!.Outcome != SurgicalOutcome.Uneventful);

        return Table("surgical-register", "Surgical register",
            "Operative procedures with outcomes and complications.", from, to,
            new[]
            {
                new ReportColumn("Date", ReportColumnType.Date),
                new ReportColumn("Patient", ReportColumnType.Text, Width: 2),
                new ReportColumn("Surgery", ReportColumnType.Text, Width: 2),
                new ReportColumn("Code", ReportColumnType.Text),
                new ReportColumn("Tooth", ReportColumnType.Text),
                new ReportColumn("Surgeon", ReportColumnType.Text, Width: 1.5),
                new ReportColumn("Minutes", ReportColumnType.Integer, Total: true),
                new ReportColumn("Graft", ReportColumnType.Boolean),
                new ReportColumn("Sutures", ReportColumnType.Integer),
                new ReportColumn("Outcome", ReportColumnType.Text),
                new ReportColumn("Complications", ReportColumnType.Text, Width: 2),
                new ReportColumn("Fee", ReportColumnType.Money, Total: true)
            },
            cases.Select(c => new object?[]
            {
                DateOnly.FromDateTime(c.DateOfService), c.Patient?.Name.Display,
                c.SurgicalRecord!.SurgeryType, c.ProcedureCode?.Code, c.Tooth?.FdiNumber,
                c.Provider?.DisplayName, c.SurgicalRecord.DurationMinutes,
                c.SurgicalRecord.GraftPlaced, c.SurgicalRecord.SutureCount,
                c.SurgicalRecord.Outcome, c.SurgicalRecord.Complications, c.NetFee
            }).ToList(),
            new[]
            {
                new ReportMetric("Cases", cases.Count.ToString("N0")),
                new ReportMetric("Complication rate",
                    $"{(cases.Count == 0 ? 0 : 100m * complications / cases.Count):0.##}%",
                    $"{complications} of {cases.Count}"),
                new ReportMetric("Value", Money(cases.Sum(c => c.NetFee)))
            });
    }

    private async Task<ReportTable> ImplantsAsync(CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var implants = await db.DentalImplants.AsNoTracking()
            .Include(i => i.Patient).Include(i => i.Tooth).Include(i => i.SurgeonStaff)
            .OrderByDescending(i => i.PlacementDate)
            .ToListAsync(ct);

        var failed = implants.Count(i => i.Status is ImplantStatus.Failed or ImplantStatus.Explanted);

        return Table("implants", "Implant registry",
            "Every fixture placed, traceable by lot number.", null, null,
            new[]
            {
                new ReportColumn("Patient", ReportColumnType.Text, Width: 2),
                new ReportColumn("Tooth", ReportColumnType.Text),
                new ReportColumn("Manufacturer", ReportColumnType.Text, Width: 1.5),
                new ReportColumn("System", ReportColumnType.Text, Width: 1.5),
                new ReportColumn("Reference", ReportColumnType.Text),
                new ReportColumn("Lot", ReportColumnType.Text),
                new ReportColumn("Diameter", ReportColumnType.Number),
                new ReportColumn("Length", ReportColumnType.Number),
                new ReportColumn("Placed", ReportColumnType.Date),
                new ReportColumn("Surgeon", ReportColumnType.Text, Width: 1.5),
                new ReportColumn("Torque", ReportColumnType.Integer),
                new ReportColumn("ISQ", ReportColumnType.Integer),
                new ReportColumn("Restored", ReportColumnType.Date),
                new ReportColumn("Status", ReportColumnType.Text)
            },
            implants.Select(i => new object?[]
            {
                i.Patient?.Name.Display, i.Tooth?.FdiNumber, i.Manufacturer, i.SystemName,
                i.ReferenceNumber, i.LotNumber, i.DiameterMm, i.LengthMm, i.PlacementDate,
                i.SurgeonStaff?.DisplayName, i.InsertionTorqueNcm, i.StabilityQuotientIsq,
                i.RestorationDate, i.Status
            }).ToList(),
            new[]
            {
                new ReportMetric("Implants placed", implants.Count.ToString("N0")),
                new ReportMetric("Restored", implants.Count(i => i.Status == ImplantStatus.Restored).ToString("N0")),
                new ReportMetric("Failure rate",
                    $"{(implants.Count == 0 ? 0 : 100m * failed / implants.Count):0.##}%", $"{failed} failed")
            });
    }

    private async Task<ReportTable> PrescriptionsAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var prescriptions = await db.Prescriptions.AsNoTracking()
            .Include(p => p.Patient).Include(p => p.PrescriberStaff)
            .Include(p => p.Items).ThenInclude(i => i.Medication)
            .Where(p => p.IssueDate >= from && p.IssueDate <= to && p.Status != PrescriptionStatus.Draft)
            .OrderByDescending(p => p.IssueDate)
            .ToListAsync(ct);

        var rows = prescriptions.SelectMany(p => p.Items.Select(item => new object?[]
        {
            p.PrescriptionNumber, p.IssueDate, p.Patient?.Name.Display, p.PrescriberStaff?.DisplayName,
            item.DisplayName, item.Sig, item.Quantity, item.Repeats,
            item.Medication?.IsControlledDrug == true, item.Medication?.IsAntibiotic == true,
            p.Indication
        })).ToList();

        var controlled = prescriptions.Count(p => p.Items.Any(i => i.Medication?.IsControlledDrug == true));

        return Table("prescriptions", "Prescriptions issued",
            "One row per prescribed item, including controlled drugs.", from, to,
            new[]
            {
                new ReportColumn("Prescription", ReportColumnType.Text),
                new ReportColumn("Issued", ReportColumnType.Date),
                new ReportColumn("Patient", ReportColumnType.Text, Width: 2),
                new ReportColumn("Prescriber", ReportColumnType.Text, Width: 1.5),
                new ReportColumn("Medication", ReportColumnType.Text, Width: 2),
                new ReportColumn("Directions", ReportColumnType.Text, Width: 2.5),
                new ReportColumn("Quantity", ReportColumnType.Number, Total: true),
                new ReportColumn("Repeats", ReportColumnType.Integer),
                new ReportColumn("Controlled", ReportColumnType.Boolean),
                new ReportColumn("Antibiotic", ReportColumnType.Boolean),
                new ReportColumn("Indication", ReportColumnType.Text, Width: 1.5)
            },
            rows,
            new[]
            {
                new ReportMetric("Prescriptions", prescriptions.Count.ToString("N0")),
                new ReportMetric("Items", rows.Count.ToString("N0")),
                new ReportMetric("Controlled drugs", controlled.ToString("N0"))
            });
    }

    private async Task<ReportTable> RadiographsAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var start = from.ToDateTime(TimeOnly.MinValue);
        var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var records = await db.RadiographRecords.AsNoTracking()
            .Include(r => r.Patient).Include(r => r.TakenByStaff)
            .Where(r => r.TakenAtUtc >= start && r.TakenAtUtc < end)
            .OrderByDescending(r => r.TakenAtUtc)
            .ToListAsync(ct);

        var repeats = records.Count(r => r.IsRepeat);

        return Table("radiographs", "Imaging and dose",
            "Radiographic exposures with justification, dose and quality grading.", from, to,
            new[]
            {
                new ReportColumn("Date", ReportColumnType.DateTime),
                new ReportColumn("Patient", ReportColumnType.Text, Width: 2),
                new ReportColumn("Type", ReportColumnType.Text, Width: 1.5),
                new ReportColumn("Teeth", ReportColumnType.Text, Width: 1.5),
                new ReportColumn("Taken by", ReportColumnType.Text, Width: 1.5),
                new ReportColumn("kVp", ReportColumnType.Number),
                new ReportColumn("Seconds", ReportColumnType.Number),
                new ReportColumn("Dose µSv", ReportColumnType.Number, Total: true),
                new ReportColumn("Quality", ReportColumnType.Text),
                new ReportColumn("Repeat", ReportColumnType.Boolean),
                new ReportColumn("Justification", ReportColumnType.Text, Width: 2)
            },
            records.Select(r => new object?[]
            {
                r.TakenAtUtc, r.Patient?.Name.Display, r.RadiographType, r.ToothNumbers,
                r.TakenByStaff?.DisplayName, r.KiloVoltagePeak, r.ExposureSeconds,
                r.DoseMicroSieverts, r.QualityRating, r.IsRepeat, r.JustificationReason
            }).ToList(),
            new[]
            {
                new ReportMetric("Exposures", records.Count.ToString("N0")),
                new ReportMetric("Total dose", $"{records.Sum(r => r.DoseMicroSieverts ?? 0):0.#} µSv"),
                new ReportMetric("Repeat rate",
                    $"{(records.Count == 0 ? 0 : 100m * repeats / records.Count):0.#}%", $"{repeats} repeats")
            });
    }

    private async Task<ReportTable> TreatmentAcceptanceAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var rows = await reporting.GetTreatmentAcceptanceAsync(from, to, ct);

        var presented = rows.Sum(r => r.TotalFee);
        var accepted = rows.Sum(r => r.AcceptedValue);

        return Table("treatment-acceptance", "Treatment plan acceptance",
            "Plans presented against the value accepted.", from, to,
            new[]
            {
                new ReportColumn("Plan", ReportColumnType.Text),
                new ReportColumn("Patient", ReportColumnType.Text, Width: 2),
                new ReportColumn("Provider", ReportColumnType.Text, Width: 1.5),
                new ReportColumn("Created", ReportColumnType.Date),
                new ReportColumn("Presented", ReportColumnType.Date),
                new ReportColumn("Value", ReportColumnType.Money, Total: true),
                new ReportColumn("Accepted", ReportColumnType.Money, Total: true),
                new ReportColumn("Acceptance", ReportColumnType.Percent),
                new ReportColumn("Days since presented", ReportColumnType.Integer),
                new ReportColumn("Status", ReportColumnType.Text)
            },
            rows.Select(r => new object?[]
            {
                r.PlanNumber, r.PatientName, r.ProviderName, r.CreatedOn, r.PresentedOn,
                r.TotalFee, r.AcceptedValue, r.AcceptancePercent, r.DaysSincePresented, r.Status
            }).ToList(),
            new[]
            {
                new ReportMetric("Presented value", Money(presented)),
                new ReportMetric("Accepted value", Money(accepted)),
                new ReportMetric("Acceptance rate",
                    $"{(presented == 0 ? 0 : 100m * accepted / presented):0.#}%")
            });
    }

    private async Task<ReportTable> PatientsAsync(CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var patients = await db.Patients.AsNoTracking()
            .Include(p => p.PrimaryProvider)
            .OrderBy(p => p.Name.LastName).ThenBy(p => p.Name.FirstName)
            .ToListAsync(ct);

        return Table("patients", "Patient register", "The full patient list.", null, null,
            new[]
            {
                new ReportColumn("Patient number", ReportColumnType.Text),
                new ReportColumn("Surname", ReportColumnType.Text, Width: 1.5),
                new ReportColumn("First name", ReportColumnType.Text, Width: 1.5),
                new ReportColumn("Date of birth", ReportColumnType.Date),
                new ReportColumn("Age", ReportColumnType.Integer),
                new ReportColumn("Gender", ReportColumnType.Text),
                new ReportColumn("Phone", ReportColumnType.Text),
                new ReportColumn("Email", ReportColumnType.Text, Width: 2),
                new ReportColumn("Postcode", ReportColumnType.Text),
                new ReportColumn("Provider", ReportColumnType.Text, Width: 1.5),
                new ReportColumn("Registered", ReportColumnType.Date),
                new ReportColumn("Last exam", ReportColumnType.Date),
                new ReportColumn("Recall due", ReportColumnType.Date),
                new ReportColumn("Balance", ReportColumnType.Money, Total: true),
                new ReportColumn("Status", ReportColumnType.Text)
            },
            patients.Select(p => new object?[]
            {
                p.PatientNumber, p.Name.LastName, p.Name.FirstName, p.DateOfBirth, p.AgeYears,
                p.Gender, p.Contact.BestPhone, p.Contact.Email, p.Address.PostCode,
                p.PrimaryProvider?.DisplayName, p.RegistrationDate, p.LastExamDate,
                p.NextRecallDue, p.AccountBalance, p.Status
            }).ToList(),
            new[]
            {
                new ReportMetric("Patients", patients.Count.ToString("N0")),
                new ReportMetric("Active", patients.Count(p => p.Status == PatientStatus.Active).ToString("N0")),
                new ReportMetric("Total balance", Money(patients.Sum(p => p.AccountBalance)))
            });
    }

    // ---------------------------------------------------------------- operations

    private async Task<ReportTable> StockAsync(CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var items = await db.InventoryItems.AsNoTracking()
            .Include(i => i.PreferredSupplier)
            .Where(i => i.IsActive)
            .OrderBy(i => i.Category).ThenBy(i => i.Name)
            .ToListAsync(ct);

        return Table("stock", "Stock levels", "Current stock against reorder levels.", null, null,
            new[]
            {
                new ReportColumn("SKU", ReportColumnType.Text),
                new ReportColumn("Item", ReportColumnType.Text, Width: 2.5),
                new ReportColumn("Category", ReportColumnType.Text, Width: 1.5),
                new ReportColumn("Supplier", ReportColumnType.Text, Width: 1.5),
                new ReportColumn("Location", ReportColumnType.Text, Width: 1.5),
                new ReportColumn("Unit", ReportColumnType.Text),
                new ReportColumn("In stock", ReportColumnType.Number),
                new ReportColumn("Reorder at", ReportColumnType.Number),
                new ReportColumn("Reorder qty", ReportColumnType.Number),
                new ReportColumn("Unit cost", ReportColumnType.Money),
                new ReportColumn("Value", ReportColumnType.Money, Total: true),
                new ReportColumn("Needs reorder", ReportColumnType.Boolean)
            },
            items.Select(i => new object?[]
            {
                i.Sku, i.Name, i.Category, i.PreferredSupplier?.Name, i.StorageLocation,
                i.UnitOfMeasure, i.CurrentStock, i.ReorderLevel, i.ReorderQuantity,
                i.UnitCost, i.StockValue, i.IsBelowReorderLevel
            }).ToList(),
            new[]
            {
                new ReportMetric("Lines", items.Count.ToString("N0")),
                new ReportMetric("Stock value", Money(items.Sum(i => i.StockValue))),
                new ReportMetric("Below reorder level", items.Count(i => i.IsBelowReorderLevel).ToString("N0"))
            });
    }

    private async Task<ReportTable> ExpiringStockAsync(CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var cutoff = clock.Today.AddDays(120);

        var lots = await db.InventoryLots.AsNoTracking()
            .Include(l => l.InventoryItem)
            .Where(l => l.QuantityRemaining > 0 && l.ExpiryDate != null && l.ExpiryDate <= cutoff)
            .OrderBy(l => l.ExpiryDate)
            .ToListAsync(ct);

        return Table("expiring-stock", "Expiring stock",
            "Lots at or near their expiry date.", null, null,
            new[]
            {
                new ReportColumn("SKU", ReportColumnType.Text),
                new ReportColumn("Item", ReportColumnType.Text, Width: 2.5),
                new ReportColumn("Lot", ReportColumnType.Text),
                new ReportColumn("Received", ReportColumnType.Date),
                new ReportColumn("Expires", ReportColumnType.Date),
                new ReportColumn("Days left", ReportColumnType.Integer),
                new ReportColumn("Remaining", ReportColumnType.Number),
                new ReportColumn("Value", ReportColumnType.Money, Total: true),
                new ReportColumn("Expired", ReportColumnType.Boolean)
            },
            lots.Select(l => new object?[]
            {
                l.InventoryItem?.Sku, l.InventoryItem?.Name, l.LotNumber, l.ReceivedDate,
                l.ExpiryDate, l.DaysUntilExpiry, l.QuantityRemaining,
                l.QuantityRemaining * l.UnitCost, l.IsExpired
            }).ToList(),
            new[]
            {
                new ReportMetric("Lots", lots.Count.ToString("N0")),
                new ReportMetric("Already expired", lots.Count(l => l.IsExpired).ToString("N0")),
                new ReportMetric("Value at risk", Money(lots.Sum(l => l.QuantityRemaining * l.UnitCost)))
            });
    }

    private async Task<ReportTable> SterilisationAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var start = from.ToDateTime(TimeOnly.MinValue);
        var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var cycles = await db.SterilisationCycles.AsNoTracking()
            .Include(c => c.Steriliser).Include(c => c.OperatorStaff)
            .Where(c => c.StartedAtUtc >= start && c.StartedAtUtc < end)
            .OrderByDescending(c => c.StartedAtUtc)
            .ToListAsync(ct);

        var failures = cycles.Count(c => c.Result == SterilisationResult.Fail);

        return Table("sterilisation", "Sterilisation log",
            "Cycle records with indicator results. Retained as a compliance record.", from, to,
            new[]
            {
                new ReportColumn("Started", ReportColumnType.DateTime),
                new ReportColumn("Steriliser", ReportColumnType.Text, Width: 2),
                new ReportColumn("Cycle", ReportColumnType.Integer),
                new ReportColumn("Program", ReportColumnType.Text, Width: 1.5),
                new ReportColumn("Peak °C", ReportColumnType.Number),
                new ReportColumn("Bar", ReportColumnType.Number),
                new ReportColumn("Minutes", ReportColumnType.Integer),
                new ReportColumn("Chemical", ReportColumnType.Boolean),
                new ReportColumn("Helix", ReportColumnType.Boolean),
                new ReportColumn("Vacuum", ReportColumnType.Boolean),
                new ReportColumn("Biological", ReportColumnType.Text),
                new ReportColumn("Items", ReportColumnType.Integer, Total: true),
                new ReportColumn("Operator", ReportColumnType.Text, Width: 1.5),
                new ReportColumn("Result", ReportColumnType.Text)
            },
            cycles.Select(c => new object?[]
            {
                c.StartedAtUtc, c.Steriliser?.Name, c.CycleNumber, c.Program,
                c.PeakTemperatureCelsius, c.PeakPressureBar, c.DurationMinutes,
                c.ChemicalIndicatorPass, c.HelixTestPass, c.VacuumLeakTestPass,
                c.BiologicalIndicatorPass is null ? "Not tested" : c.BiologicalIndicatorPass.Value ? "Pass" : "Fail",
                c.ItemCount, c.OperatorStaff?.DisplayName, c.Result
            }).ToList(),
            new[]
            {
                new ReportMetric("Cycles", cycles.Count.ToString("N0")),
                new ReportMetric("Failures", failures.ToString("N0"),
                    $"{(cycles.Count == 0 ? 0 : 100m * failures / cycles.Count):0.##}% failure rate"),
                new ReportMetric("Items processed", cycles.Sum(c => c.ItemCount).ToString("N0"))
            });
    }

    private async Task<ReportTable> LabCasesAsync(CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var cases = await db.LabCases.AsNoTracking()
            .Include(c => c.Patient).Include(c => c.DentalLaboratory).Include(c => c.Provider)
            .OrderBy(c => c.DueDate)
            .ToListAsync(ct);

        return Table("lab-cases", "Laboratory cases",
            "Work sent out, due dates and remakes.", null, null,
            new[]
            {
                new ReportColumn("Case", ReportColumnType.Text),
                new ReportColumn("Patient", ReportColumnType.Text, Width: 2),
                new ReportColumn("Type", ReportColumnType.Text, Width: 1.5),
                new ReportColumn("Teeth", ReportColumnType.Text),
                new ReportColumn("Shade", ReportColumnType.Text),
                new ReportColumn("Laboratory", ReportColumnType.Text, Width: 2),
                new ReportColumn("Provider", ReportColumnType.Text, Width: 1.5),
                new ReportColumn("Sent", ReportColumnType.Date),
                new ReportColumn("Due", ReportColumnType.Date),
                new ReportColumn("Received", ReportColumnType.Date),
                new ReportColumn("Fee", ReportColumnType.Money, Total: true),
                new ReportColumn("Remake", ReportColumnType.Boolean),
                new ReportColumn("Status", ReportColumnType.Text)
            },
            cases.Select(c => new object?[]
            {
                c.CaseNumber, c.Patient?.Name.Display, c.CaseType, c.ToothNumbers, c.Shade,
                c.DentalLaboratory?.Name, c.Provider?.DisplayName,
                c.SentDate, c.DueDate, c.ReceivedDate, c.LabFee, c.IsRemake, c.Status
            }).ToList(),
            new[]
            {
                new ReportMetric("Cases", cases.Count.ToString("N0")),
                new ReportMetric("Overdue", cases.Count(c => c.IsOverdue).ToString("N0")),
                new ReportMetric("Laboratory spend", Money(cases.Sum(c => c.LabFee ?? 0)))
            });
    }

    private async Task<ReportTable> AuditAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var start = from.ToDateTime(TimeOnly.MinValue);
        var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var entries = await db.AuditLogs.AsNoTracking()
            .Where(a => a.TimestampUtc >= start && a.TimestampUtc < end)
            .OrderByDescending(a => a.TimestampUtc)
            .Take(20000)
            .ToListAsync(ct);

        return Table("audit", "Audit trail",
            "Record changes with the user who made them. Values are omitted; open the entry in the application to see them.",
            from, to,
            new[]
            {
                new ReportColumn("When", ReportColumnType.DateTime),
                new ReportColumn("Action", ReportColumnType.Text),
                new ReportColumn("Entity", ReportColumnType.Text, Width: 1.5),
                new ReportColumn("Record", ReportColumnType.Text, Width: 2),
                new ReportColumn("User", ReportColumnType.Text, Width: 1.5),
                new ReportColumn("IP address", ReportColumnType.Text),
                new ReportColumn("Changed columns", ReportColumnType.Text, Width: 3)
            },
            entries.Select(a => new object?[]
            {
                a.TimestampUtc, a.Action, a.EntityName, a.EntityId, a.UserName, a.IpAddress, a.ChangedColumns
            }).ToList(),
            new[]
            {
                new ReportMetric("Entries", entries.Count.ToString("N0")),
                new ReportMetric("Users", entries.Select(a => a.UserName).Distinct().Count().ToString("N0")),
                new ReportMetric("Deletions", entries.Count(a => a.Action == AuditAction.Delete).ToString("N0"))
            });
    }

    // ---------------------------------------------------------------- helpers

    private ReportTable Table(
        string key, string title, string subtitle, DateOnly? from, DateOnly? to,
        IReadOnlyList<ReportColumn> columns, IReadOnlyList<object?[]> rows,
        IReadOnlyList<ReportMetric>? metrics = null) =>
        new()
        {
            Key = key,
            Title = title,
            Subtitle = subtitle,
            From = from,
            To = to,
            Columns = columns,
            Rows = rows,
            Metrics = metrics ?? Array.Empty<ReportMetric>(),
            GeneratedAtUtc = clock.UtcNow,
            GeneratedBy = currentUser.DisplayName ?? currentUser.UserName
        };

    private static string Money(decimal value) => $"£{value:N2}";
}
