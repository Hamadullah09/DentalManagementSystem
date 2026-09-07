using DentalSurgery.Application.Abstractions;
using DentalSurgery.Application.Billing;
using DentalSurgery.Application.Reporting;
using DentalSurgery.Application.Scheduling;
using DentalSurgery.Domain.Enums;
using DentalSurgery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DentalSurgery.Infrastructure.Services;

/// <summary>Builds the dashboard and the management reports.</summary>
public class ReportingService(DentalDbContext db, IDateTimeProvider clock)
{
    private readonly LedgerCalculator _ledger = new();
    private readonly AvailabilityCalculator _availability = new();

    // ------------------------------------------------------------------ dashboard

    public async Task<PracticeDashboard> GetDashboardAsync(
        DateOnly? forDate = null, Guid? locationId = null, CancellationToken ct = default)
    {
        var date = forDate ?? clock.Today;
        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var dayEnd = dayStart.AddDays(1);
        var monthStart = new DateOnly(date.Year, date.Month, 1);
        var monthStartUtc = monthStart.ToDateTime(TimeOnly.MinValue);

        var appointments = db.Appointments.AsNoTracking().AsQueryable();
        if (locationId.HasValue) appointments = appointments.Where(a => a.LocationId == locationId);

        var todaysAppointments = await appointments
            .Where(a => a.StartUtc >= dayStart && a.StartUtc < dayEnd)
            .ToListAsync(ct);

        var tomorrowStart = dayEnd;
        var tomorrowEnd = dayEnd.AddDays(1);
        var unconfirmedTomorrow = await appointments
            .CountAsync(a => a.StartUtc >= tomorrowStart && a.StartUtc < tomorrowEnd &&
                             a.Status == AppointmentStatus.Unconfirmed, ct);

        var monthAppointments = await appointments
            .Where(a => a.StartUtc >= monthStartUtc && a.StartUtc < dayEnd)
            .Select(a => new { a.Status })
            .ToListAsync(ct);

        // --- production and collections -------------------------------------
        var todaysProcedures = await db.Procedures.AsNoTracking()
            .Where(p => p.DateOfService >= dayStart && p.DateOfService < dayEnd &&
                        p.Status == ProcedureStatus.Completed)
            .Select(p => new { p.Fee, p.Quantity, p.DiscountAmount, p.ProviderId })
            .ToListAsync(ct);

        var monthProcedures = await db.Procedures.AsNoTracking()
            .Where(p => p.DateOfService >= monthStartUtc && p.DateOfService < dayEnd &&
                        p.Status == ProcedureStatus.Completed)
            .Select(p => new { p.Fee, p.Quantity, p.DiscountAmount, p.DateOfService, p.ProviderId })
            .ToListAsync(ct);

        var todaysPayments = await db.Payments.AsNoTracking()
            .Where(p => p.PaymentDate == date && p.Status == PaymentStatus.Cleared)
            .SumAsync(p => (decimal?)(p.Amount - p.RefundedAmount), ct) ?? 0m;

        var monthPayments = await db.Payments.AsNoTracking()
            .Where(p => p.PaymentDate >= monthStart && p.PaymentDate <= date && p.Status == PaymentStatus.Cleared)
            .SumAsync(p => (decimal?)(p.Amount - p.RefundedAmount), ct) ?? 0m;

        var outstanding = await db.Patients.AsNoTracking()
            .Where(p => p.AccountBalance > 0)
            .SumAsync(p => (decimal?)p.AccountBalance, ct) ?? 0m;

        var insurancePending = await db.InsuranceClaims.AsNoTracking()
            .Where(c => c.Status != ClaimStatus.Paid && c.Status != ClaimStatus.Closed && c.Status != ClaimStatus.Denied)
            .SumAsync(c => (decimal?)(c.TotalCharged - c.TotalPaid), ct) ?? 0m;

        // --- patients and recalls --------------------------------------------
        var activePatients = await db.Patients.CountAsync(p => p.Status == PatientStatus.Active, ct);
        var newThisMonth = await db.Patients.CountAsync(p => p.RegistrationDate >= monthStart, ct);
        var recallsDue = await db.RecallSchedules.CountAsync(
            r => r.IsActive && r.DueDate <= date.AddDays(30) && r.Status != RecallStatus.Booked, ct);
        var recallsOverdue = await db.RecallSchedules.CountAsync(
            r => r.IsActive && r.DueDate < date && r.Status != RecallStatus.Booked, ct);

        // --- treatment planning -----------------------------------------------
        var awaitingDecision = await db.TreatmentPlans.CountAsync(
            p => p.Status == TreatmentPlanStatus.Presented, ct);

        var recentPlans = await db.TreatmentPlans.AsNoTracking()
            .Where(p => p.PresentedOn != null && p.PresentedOn >= date.AddMonths(-3))
            .Select(p => new { p.TotalFee, p.Status })
            .ToListAsync(ct);

        var presentedValue = recentPlans.Sum(p => p.TotalFee);
        var acceptedValue = recentPlans
            .Where(p => p.Status is TreatmentPlanStatus.Accepted or TreatmentPlanStatus.PartiallyAccepted
                or TreatmentPlanStatus.InProgress or TreatmentPlanStatus.Completed)
            .Sum(p => p.TotalFee);

        // --- operations --------------------------------------------------------
        var lowStock = await db.InventoryItems.CountAsync(
            i => i.IsActive && i.CurrentStock <= i.ReorderLevel, ct);
        var expiringCutoff = date.AddDays(90);
        var expiring = await db.InventoryLots.CountAsync(
            l => l.QuantityRemaining > 0 && l.ExpiryDate != null && l.ExpiryDate <= expiringCutoff, ct);
        var labsDue = await db.LabCases.CountAsync(
            c => c.DueDate != null && c.DueDate <= date.AddDays(7) &&
                 c.Status != LabCaseStatus.Delivered && c.Status != LabCaseStatus.Cancelled, ct);
        var labsOverdue = await db.LabCases.CountAsync(
            c => c.DueDate != null && c.DueDate < date &&
                 c.Status != LabCaseStatus.Received && c.Status != LabCaseStatus.Delivered &&
                 c.Status != LabCaseStatus.Cancelled, ct);
        var openTasks = await db.WorkTasks.CountAsync(t => !t.IsCompleted, ct);
        var sterilisationFailures = await db.SterilisationCycles.CountAsync(
            c => c.Result == SterilisationResult.Fail && c.StartedAtUtc >= monthStartUtc, ct);

        // --- provider breakdown -------------------------------------------------
        var providers = await db.Staff.AsNoTracking()
            .Where(s => s.IsActive && s.IsProvider)
            .Select(s => new { s.Id, First = s.Name.FirstName, Last = s.Name.LastName })
            .ToListAsync(ct);

        var rota = await db.StaffScheduleSlots.AsNoTracking().Where(r => r.IsActive).ToListAsync(ct);
        var todayForUtilisation = await db.Appointments.AsNoTracking()
            .Where(a => a.StartUtc >= dayStart && a.StartUtc < dayEnd).ToListAsync(ct);

        var providerRows = providers.Select(p =>
        {
            var appts = todaysAppointments.Where(a => a.ProviderId == p.Id && a.IsActiveBooking).ToList();
            var production = todaysProcedures
                .Where(x => x.ProviderId == p.Id)
                .Sum(x => (x.Fee * x.Quantity) - x.DiscountAmount);

            return new ProviderProductionRow(
                p.Id, $"{p.First} {p.Last}".Trim(),
                appts.Count,
                todaysProcedures.Count(x => x.ProviderId == p.Id),
                Math.Round(production, 2), 0m,
                _availability.CalculateUtilisation(date, p.Id, todayForUtilisation, rota));
        })
        .Where(r => r.Appointments > 0 || r.Production > 0 || r.UtilisationPercent > 0)
        .OrderByDescending(r => r.Production)
        .ToList();

        // --- 30-day production trend ---------------------------------------------
        var trendStart = date.AddDays(-29);
        var trendStartUtc = trendStart.ToDateTime(TimeOnly.MinValue);
        var trendRaw = await db.Procedures.AsNoTracking()
            .Where(p => p.DateOfService >= trendStartUtc && p.DateOfService < dayEnd &&
                        p.Status == ProcedureStatus.Completed)
            .Select(p => new { p.DateOfService, Amount = (p.Fee * p.Quantity) - p.DiscountAmount })
            .ToListAsync(ct);

        var trend = Enumerable.Range(0, 30)
            .Select(offset =>
            {
                var d = trendStart.AddDays(offset);
                var rows = trendRaw.Where(r => DateOnly.FromDateTime(r.DateOfService) == d).ToList();
                return new DailySeriesPoint(d, Math.Round(rows.Sum(r => r.Amount), 2), rows.Count);
            })
            .ToList();

        var utilisation = providerRows.Count == 0 ? 0m
            : Math.Round(providerRows.Average(r => r.UtilisationPercent), 1);

        return new PracticeDashboard
        {
            Date = date,
            AppointmentsToday = todaysAppointments.Count(a => a.IsActiveBooking),
            PatientsSeenToday = todaysAppointments.Count(a => a.Status is AppointmentStatus.Completed or AppointmentStatus.CheckedOut),
            WaitingNow = todaysAppointments.Count(a => a.Status == AppointmentStatus.ArrivedWaiting),
            InTreatmentNow = todaysAppointments.Count(a => a.Status is AppointmentStatus.Seated or AppointmentStatus.InTreatment),
            UnconfirmedTomorrow = unconfirmedTomorrow,
            NoShowsThisMonth = monthAppointments.Count(a => a.Status == AppointmentStatus.NoShow),
            CancellationsThisMonth = monthAppointments.Count(a => a.Status == AppointmentStatus.Cancelled),
            ProductionToday = Math.Round(todaysProcedures.Sum(p => (p.Fee * p.Quantity) - p.DiscountAmount), 2),
            CollectionsToday = Math.Round(todaysPayments, 2),
            ProductionMonthToDate = Math.Round(monthProcedures.Sum(p => (p.Fee * p.Quantity) - p.DiscountAmount), 2),
            CollectionsMonthToDate = Math.Round(monthPayments, 2),
            OutstandingReceivables = Math.Round(outstanding, 2),
            InsurancePending = Math.Round(insurancePending, 2),
            ActivePatients = activePatients,
            NewPatientsThisMonth = newThisMonth,
            RecallsDue = recallsDue,
            RecallsOverdue = recallsOverdue,
            TreatmentPlansAwaitingDecision = awaitingDecision,
            TreatmentAcceptanceRate = presentedValue == 0m ? 0m : Math.Round(100m * acceptedValue / presentedValue, 1),
            LowStockItems = lowStock,
            ExpiringStockItems = expiring,
            LabCasesDue = labsDue,
            LabCasesOverdue = labsOverdue,
            OpenTasks = openTasks,
            SterilisationFailures = sterilisationFailures,
            ChairUtilisationPercent = utilisation,
            ProviderProduction = providerRows,
            ProductionTrend = trend,
            Alerts = BuildAlerts(recallsOverdue, lowStock, expiring, labsOverdue, sterilisationFailures,
                unconfirmedTomorrow, outstanding)
        };
    }

    private static List<AlertRow> BuildAlerts(
        int recallsOverdue, int lowStock, int expiring, int labsOverdue,
        int sterilisationFailures, int unconfirmedTomorrow, decimal outstanding)
    {
        var alerts = new List<AlertRow>();

        if (sterilisationFailures > 0)
            alerts.Add(new AlertRow("danger", "Sterilisation failure",
                $"{sterilisationFailures} failed cycle(s) this month. Trace and recall affected instrument sets.",
                "/sterilisation"));

        if (labsOverdue > 0)
            alerts.Add(new AlertRow("danger", "Laboratory work overdue",
                $"{labsOverdue} case(s) are past their due date.", "/lab-cases"));

        if (lowStock > 0)
            alerts.Add(new AlertRow("warning", "Stock at reorder level",
                $"{lowStock} item(s) need reordering.", "/inventory"));

        if (expiring > 0)
            alerts.Add(new AlertRow("warning", "Stock expiring",
                $"{expiring} lot(s) expire within 90 days.", "/inventory/expiring"));

        if (unconfirmedTomorrow > 0)
            alerts.Add(new AlertRow("info", "Unconfirmed appointments",
                $"{unconfirmedTomorrow} booking(s) tomorrow are still unconfirmed.", "/schedule"));

        if (recallsOverdue > 0)
            alerts.Add(new AlertRow("info", "Recalls overdue",
                $"{recallsOverdue} patient(s) are overdue for review.", "/recalls"));

        if (outstanding > 0)
            alerts.Add(new AlertRow("info", "Outstanding balances",
                $"{outstanding:C} is owed across all accounts.", "/billing/receivables"));

        return alerts;
    }

    // ------------------------------------------------------------------ financial

    public async Task<FinancialSummary> GetFinancialSummaryAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var startUtc = from.ToDateTime(TimeOnly.MinValue);
        var endUtc = to.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var procedures = await db.Procedures.AsNoTracking()
            .Include(p => p.ProcedureCode)
            .Where(p => p.DateOfService >= startUtc && p.DateOfService < endUtc &&
                        p.Status == ProcedureStatus.Completed)
            .Select(p => new
            {
                p.DateOfService,
                p.Fee,
                p.Quantity,
                p.DiscountAmount,
                Category = p.ProcedureCode!.Category,
                CodeDescription = p.ProcedureCode.ShortDescription
            })
            .ToListAsync(ct);

        var payments = await db.Payments.AsNoTracking()
            .Where(p => p.PaymentDate >= from && p.PaymentDate <= to)
            .Select(p => new { p.PaymentDate, p.Amount, p.RefundedAmount, p.Method, p.Status })
            .ToListAsync(ct);

        var cleared = payments.Where(p => p.Status == PaymentStatus.Cleared).ToList();

        var writeOffs = await db.AccountAdjustments.AsNoTracking()
            .Where(a => a.AdjustmentDate >= from && a.AdjustmentDate <= to &&
                        (a.AdjustmentType == AdjustmentType.BadDebt ||
                         a.AdjustmentType == AdjustmentType.CourtesyWriteOff ||
                         a.AdjustmentType == AdjustmentType.InsuranceWriteOff))
            .SumAsync(a => (decimal?)a.Amount, ct) ?? 0m;

        var invoicesIssued = await db.Invoices.CountAsync(
            i => i.IssueDate >= from && i.IssueDate <= to && i.Status != InvoiceStatus.Draft, ct);

        var closingReceivables = await db.Patients.AsNoTracking()
            .Where(p => p.AccountBalance > 0)
            .SumAsync(p => (decimal?)p.AccountBalance, ct) ?? 0m;

        var byCategory = procedures
            .GroupBy(p => p.Category)
            .Select(g => new CategoryTotal(
                Humanise(g.Key.ToString()),
                Math.Round(g.Sum(p => (p.Fee * p.Quantity) - p.DiscountAmount), 2),
                g.Count()))
            .OrderByDescending(c => c.Amount)
            .ToList();

        var byMethod = cleared
            .GroupBy(p => p.Method)
            .Select(g => new CategoryTotal(
                Humanise(g.Key.ToString()),
                Math.Round(g.Sum(p => p.Amount - p.RefundedAmount), 2),
                g.Count()))
            .OrderByDescending(c => c.Amount)
            .ToList();

        var days = Enumerable.Range(0, to.DayNumber - from.DayNumber + 1)
            .Select(offset => from.AddDays(offset))
            .ToList();

        return new FinancialSummary
        {
            From = from,
            To = to,
            GrossProduction = Math.Round(procedures.Sum(p => p.Fee * p.Quantity), 2),
            Discounts = Math.Round(procedures.Sum(p => p.DiscountAmount), 2),
            WriteOffs = Math.Round(writeOffs, 2),
            PatientCollections = Math.Round(cleared.Where(p => p.Method != PaymentMethod.Insurance)
                .Sum(p => p.Amount - p.RefundedAmount), 2),
            InsuranceCollections = Math.Round(cleared.Where(p => p.Method == PaymentMethod.Insurance)
                .Sum(p => p.Amount - p.RefundedAmount), 2),
            Refunds = Math.Round(payments.Sum(p => p.RefundedAmount), 2),
            InvoicesIssued = invoicesIssued,
            PaymentsReceived = cleared.Count,
            ClosingReceivables = Math.Round(closingReceivables, 2),
            ByCategory = byCategory,
            ByPaymentMethod = byMethod,
            DailyProduction = days.Select(d =>
            {
                var rows = procedures.Where(p => DateOnly.FromDateTime(p.DateOfService) == d).ToList();
                return new DailySeriesPoint(d, Math.Round(rows.Sum(p => (p.Fee * p.Quantity) - p.DiscountAmount), 2), rows.Count);
            }).ToList(),
            DailyCollections = days.Select(d =>
            {
                var rows = cleared.Where(p => p.PaymentDate == d).ToList();
                return new DailySeriesPoint(d, Math.Round(rows.Sum(p => p.Amount - p.RefundedAmount), 2), rows.Count);
            }).ToList()
        };
    }

    public async Task<AgingReport> GetAgingAsync(CancellationToken ct = default)
    {
        var invoices = await db.Invoices.AsNoTracking()
            .Where(i => i.Status != InvoiceStatus.Void && i.Status != InvoiceStatus.Draft)
            .ToListAsync(ct);

        return _ledger.BuildAging(invoices, clock.Today);
    }

    public async Task<List<ReceivableRow>> GetReceivablesAsync(CancellationToken ct = default)
    {
        var patients = await db.Patients.AsNoTracking()
            .Where(p => p.AccountBalance > 0.005m)
            .Select(p => new
            {
                p.Id, p.PatientNumber,
                Name = (p.Name.PreferredName ?? p.Name.FirstName) + " " + p.Name.LastName,
                Phone = p.Contact.MobilePhone ?? p.Contact.HomePhone,
                p.AccountBalance
            })
            .ToListAsync(ct);

        var ids = patients.Select(p => p.Id).ToList();

        var invoices = await db.Invoices.AsNoTracking()
            .Where(i => ids.Contains(i.PatientId) &&
                        i.Status != InvoiceStatus.Void && i.Status != InvoiceStatus.Draft)
            .ToListAsync(ct);

        var lastPayments = await db.Payments.AsNoTracking()
            .Where(p => ids.Contains(p.PatientId) && p.Status == PaymentStatus.Cleared)
            .GroupBy(p => p.PatientId)
            .Select(g => new { PatientId = g.Key, Last = g.Max(p => p.PaymentDate) })
            .ToListAsync(ct);

        var today = clock.Today;

        return patients.Select(p =>
        {
            var theirs = invoices.Where(i => i.PatientId == p.Id && i.Balance > 0.005m).ToList();
            decimal Bucket(int lower, int upper) => theirs
                .Where(i => { var d = today.DayNumber - i.DueDate.DayNumber; return d > lower && d <= upper; })
                .Sum(i => i.Balance);

            return new ReceivableRow(
                p.Id, p.PatientNumber, p.Name, p.Phone,
                Math.Round(p.AccountBalance, 2),
                Math.Round(theirs.Where(i => i.DueDate >= today).Sum(i => i.Balance), 2),
                Math.Round(Bucket(0, 30), 2),
                Math.Round(Bucket(30, 60), 2),
                Math.Round(Bucket(60, 90), 2),
                Math.Round(theirs.Where(i => today.DayNumber - i.DueDate.DayNumber > 90).Sum(i => i.Balance), 2),
                theirs.Count == 0 ? null : theirs.Min(i => i.DueDate),
                lastPayments.FirstOrDefault(l => l.PatientId == p.Id)?.Last);
        })
        .OrderByDescending(r => r.Balance)
        .ToList();
    }

    // ------------------------------------------------------------------ schedule

    public async Task<ScheduleAnalytics> GetScheduleAnalyticsAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var startUtc = from.ToDateTime(TimeOnly.MinValue);
        var endUtc = to.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var appointments = await db.Appointments.AsNoTracking()
            .Include(a => a.Provider)
            .Where(a => a.StartUtc >= startUtc && a.StartUtc < endUtc)
            .ToListAsync(ct);

        var rota = await db.StaffScheduleSlots.AsNoTracking().Where(r => r.IsActive).ToListAsync(ct);
        var providers = await db.Staff.AsNoTracking().Where(s => s.IsProvider && s.IsActive).ToListAsync(ct);

        var availableMinutes = 0;
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            foreach (var slot in rota.Where(r => r.CoversDate(date)))
            {
                var minutes = (int)(slot.EndTime - slot.StartTime).TotalMinutes;
                if (slot.BreakStart.HasValue && slot.BreakEnd.HasValue)
                    minutes -= (int)(slot.BreakEnd.Value - slot.BreakStart.Value).TotalMinutes;
                availableMinutes += Math.Max(0, minutes);
            }
        }

        var waits = appointments.Where(a => a.WaitingMinutes.HasValue).Select(a => a.WaitingMinutes!.Value).ToList();
        var chairs = appointments.Where(a => a.ChairMinutes.HasValue).Select(a => a.ChairMinutes!.Value).ToList();

        var byProvider = providers.Select(p =>
        {
            var theirs = appointments.Where(a => a.ProviderId == p.Id).ToList();
            var booked = theirs.Where(a => a.IsActiveBooking).Sum(a => a.DurationMinutes);
            var theirAvailable = 0;
            for (var date = from; date <= to; date = date.AddDays(1))
                foreach (var slot in rota.Where(r => r.StaffId == p.Id && r.CoversDate(date)))
                    theirAvailable += (int)(slot.EndTime - slot.StartTime).TotalMinutes;

            return new ProviderProductionRow(
                p.Id, p.DisplayName, theirs.Count(a => a.IsActiveBooking), 0, 0m, 0m,
                theirAvailable == 0 ? 0m : Math.Round(100m * booked / theirAvailable, 1));
        })
        .Where(r => r.Appointments > 0)
        .OrderByDescending(r => r.UtilisationPercent)
        .ToList();

        return new ScheduleAnalytics
        {
            From = from,
            To = to,
            TotalAppointments = appointments.Count,
            Completed = appointments.Count(a => a.Status is AppointmentStatus.Completed or AppointmentStatus.CheckedOut),
            Cancelled = appointments.Count(a => a.Status == AppointmentStatus.Cancelled),
            NoShows = appointments.Count(a => a.Status == AppointmentStatus.NoShow),
            Rescheduled = appointments.Count(a => a.Status == AppointmentStatus.Rescheduled),
            TotalBookedMinutes = appointments.Where(a => a.IsActiveBooking).Sum(a => a.DurationMinutes),
            TotalAvailableMinutes = availableMinutes,
            AverageWaitMinutes = waits.Count == 0 ? 0 : Math.Round(waits.Average(), 1),
            AverageChairMinutes = chairs.Count == 0 ? 0 : Math.Round(chairs.Average(), 1),
            ByAppointmentType = appointments
                .GroupBy(a => a.AppointmentType)
                .Select(g => new CategoryTotal(Humanise(g.Key.ToString()), 0m, g.Count()))
                .OrderByDescending(c => c.Count)
                .ToList(),
            ByProvider = byProvider,
            DailyVolume = Enumerable.Range(0, to.DayNumber - from.DayNumber + 1)
                .Select(offset =>
                {
                    var d = from.AddDays(offset);
                    return new DailySeriesPoint(d, 0m, appointments.Count(a => a.Date == d && a.IsActiveBooking));
                })
                .ToList()
        };
    }

    // ------------------------------------------------------------------ clinical

    public async Task<ClinicalAnalytics> GetClinicalAnalyticsAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var startUtc = from.ToDateTime(TimeOnly.MinValue);
        var endUtc = to.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var procedures = await db.Procedures.AsNoTracking()
            .Include(p => p.ProcedureCode)
            .Include(p => p.SurgicalRecord)
            .Where(p => p.DateOfService >= startUtc && p.DateOfService < endUtc &&
                        p.Status == ProcedureStatus.Completed)
            .ToListAsync(ct);

        var surgical = procedures.Where(p => p.SurgicalRecord is not null).ToList();

        return new ClinicalAnalytics
        {
            From = from,
            To = to,
            ProceduresCompleted = procedures.Count,
            SurgicalProcedures = surgical.Count,
            ImplantsPlaced = await db.DentalImplants.CountAsync(
                i => i.PlacementDate >= from && i.PlacementDate <= to, ct),
            ExtractionsPerformed = procedures.Count(p => p.ProcedureCode!.Code.StartsWith("D71") ||
                                                          p.ProcedureCode.Code.StartsWith("D72")),
            RootCanalsCompleted = procedures.Count(p => p.ProcedureCode!.Category == ProcedureCategory.Endodontics),
            RestorationsPlaced = procedures.Count(p => p.ProcedureCode!.Category == ProcedureCategory.Restorative),
            HygieneVisits = procedures.Count(p => p.ProcedureCode!.IsHygieneProcedure),
            RadiographsTaken = await db.RadiographRecords.CountAsync(
                r => r.TakenAtUtc >= startUtc && r.TakenAtUtc < endUtc, ct),
            PrescriptionsIssued = await db.Prescriptions.CountAsync(
                p => p.IssueDate >= from && p.IssueDate <= to && p.Status != PrescriptionStatus.Draft, ct),
            ComplicationCount = surgical.Count(p => p.SurgicalRecord!.Outcome != SurgicalOutcome.Uneventful),
            RedoProcedures = procedures.Count(p => p.IsWarrantyRedo),
            ReferralsOut = await db.Referrals.CountAsync(
                r => r.Direction == ReferralDirection.Outbound && r.ReferralDate >= from && r.ReferralDate <= to, ct),
            ByProcedureCategory = procedures
                .GroupBy(p => p.ProcedureCode!.Category)
                .Select(g => new CategoryTotal(
                    Humanise(g.Key.ToString()),
                    Math.Round(g.Sum(p => p.NetFee), 2), g.Count()))
                .OrderByDescending(c => c.Count)
                .ToList(),
            TopProcedures = procedures
                .GroupBy(p => p.ProcedureCode!.ShortDescription)
                .Select(g => new CategoryTotal(g.Key, Math.Round(g.Sum(p => p.NetFee), 2), g.Count()))
                .OrderByDescending(c => c.Count)
                .Take(15)
                .ToList(),
            BySurgeryType = surgical
                .GroupBy(p => p.SurgicalRecord!.SurgeryType)
                .Select(g => new CategoryTotal(Humanise(g.Key.ToString()), Math.Round(g.Sum(p => p.NetFee), 2), g.Count()))
                .OrderByDescending(c => c.Count)
                .ToList()
        };
    }

    public async Task<List<TreatmentAcceptanceRow>> GetTreatmentAcceptanceAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var plans = await db.TreatmentPlans.AsNoTracking()
            .Include(p => p.Patient)
            .Include(p => p.Provider)
            .Include(p => p.Phases).ThenInclude(ph => ph.Items)
            .Where(p => p.CreatedOn >= from && p.CreatedOn <= to && p.Status != TreatmentPlanStatus.Draft)
            .ToListAsync(ct);

        return plans.Select(p => new TreatmentAcceptanceRow(
            p.Id, p.PlanNumber, p.PatientId,
            p.Patient is null ? "Unknown" : p.Patient.Name.Display,
            p.Provider?.DisplayName,
            p.CreatedOn, p.PresentedOn, p.Status,
            Math.Round(p.TotalFee, 2), Math.Round(p.AcceptedValue, 2)))
            .OrderByDescending(r => r.TotalFee)
            .ToList();
    }

    public async Task<List<RecallDueRow>> GetRecallsDueAsync(
        DateOnly? dueBy = null, bool includeBooked = false, CancellationToken ct = default)
    {
        var cutoff = dueBy ?? clock.Today.AddDays(30);

        var query = db.RecallSchedules.AsNoTracking()
            .Include(r => r.Patient)
            .Include(r => r.PreferredProvider)
            .Where(r => r.IsActive && r.DueDate <= cutoff);

        if (!includeBooked) query = query.Where(r => r.Status != RecallStatus.Booked);

        var recalls = await query.OrderBy(r => r.DueDate).ToListAsync(ct);

        return recalls
            .Where(r => r.Patient is not null && r.Patient.Status == PatientStatus.Active)
            .Select(r => new RecallDueRow(
                r.Id, r.PatientId, r.Patient!.PatientNumber, r.Patient.Name.Display,
                r.Patient.Contact.BestPhone, r.Patient.Contact.Email,
                r.RecallType, r.DueDate, r.Status, r.ContactAttempts,
                r.PreferredProvider?.DisplayName))
            .ToList();
    }

    /// <summary>Turns an enum name into spaced words for display.</summary>
    private static string Humanise(string pascalCase) =>
        System.Text.RegularExpressions.Regex.Replace(pascalCase, "(?<!^)([A-Z])", " $1");
}
