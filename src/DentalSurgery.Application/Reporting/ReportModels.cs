using DentalSurgery.Domain.Enums;

namespace DentalSurgery.Application.Reporting;

/// <summary>Headline figures for the dashboard.</summary>
public class PracticeDashboard
{
    public DateOnly Date { get; init; }

    public int AppointmentsToday { get; init; }
    public int PatientsSeenToday { get; init; }
    public int WaitingNow { get; init; }
    public int InTreatmentNow { get; init; }
    public int UnconfirmedTomorrow { get; init; }
    public int NoShowsThisMonth { get; init; }
    public int CancellationsThisMonth { get; init; }

    public decimal ProductionToday { get; init; }
    public decimal CollectionsToday { get; init; }
    public decimal ProductionMonthToDate { get; init; }
    public decimal CollectionsMonthToDate { get; init; }
    public decimal OutstandingReceivables { get; init; }
    public decimal InsurancePending { get; init; }

    public int ActivePatients { get; init; }
    public int NewPatientsThisMonth { get; init; }
    public int RecallsDue { get; init; }
    public int RecallsOverdue { get; init; }

    public int TreatmentPlansAwaitingDecision { get; init; }
    public decimal TreatmentAcceptanceRate { get; init; }

    public int LowStockItems { get; init; }
    public int ExpiringStockItems { get; init; }
    public int LabCasesDue { get; init; }
    public int LabCasesOverdue { get; init; }
    public int OpenTasks { get; init; }
    public int SterilisationFailures { get; init; }

    public decimal ChairUtilisationPercent { get; init; }

    public IReadOnlyList<ProviderProductionRow> ProviderProduction { get; init; } = Array.Empty<ProviderProductionRow>();
    public IReadOnlyList<DailySeriesPoint> ProductionTrend { get; init; } = Array.Empty<DailySeriesPoint>();
    public IReadOnlyList<AlertRow> Alerts { get; init; } = Array.Empty<AlertRow>();
}

public record ProviderProductionRow(
    Guid ProviderId,
    string ProviderName,
    int Appointments,
    int Procedures,
    decimal Production,
    decimal Collections,
    decimal UtilisationPercent)
{
    public decimal AverageProductionPerAppointment =>
        Appointments == 0 ? 0m : Math.Round(Production / Appointments, 2);
}

public record DailySeriesPoint(DateOnly Date, decimal Value, int Count = 0);

public record AlertRow(string Severity, string Title, string Detail, string? Link = null);

/// <summary>Production and collection totals for a period.</summary>
public class FinancialSummary
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }

    public decimal GrossProduction { get; init; }
    public decimal Discounts { get; init; }
    public decimal WriteOffs { get; init; }
    public decimal NetProduction => GrossProduction - Discounts - WriteOffs;

    public decimal PatientCollections { get; init; }
    public decimal InsuranceCollections { get; init; }
    public decimal TotalCollections => PatientCollections + InsuranceCollections;
    public decimal Refunds { get; init; }

    public decimal CollectionRatePercent =>
        NetProduction == 0m ? 0m : Math.Round(100m * TotalCollections / NetProduction, 1);

    public int InvoicesIssued { get; init; }
    public int PaymentsReceived { get; init; }
    public decimal OpeningReceivables { get; init; }
    public decimal ClosingReceivables { get; init; }

    public IReadOnlyList<CategoryTotal> ByCategory { get; init; } = Array.Empty<CategoryTotal>();
    public IReadOnlyList<CategoryTotal> ByPaymentMethod { get; init; } = Array.Empty<CategoryTotal>();
    public IReadOnlyList<DailySeriesPoint> DailyProduction { get; init; } = Array.Empty<DailySeriesPoint>();
    public IReadOnlyList<DailySeriesPoint> DailyCollections { get; init; } = Array.Empty<DailySeriesPoint>();
}

public record CategoryTotal(string Label, decimal Amount, int Count)
{
    public decimal PercentOf(decimal total) => total == 0m ? 0m : Math.Round(100m * Amount / total, 1);
}

/// <summary>Utilisation of the appointment book.</summary>
public class ScheduleAnalytics
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }

    public int TotalAppointments { get; init; }
    public int Completed { get; init; }
    public int Cancelled { get; init; }
    public int NoShows { get; init; }
    public int Rescheduled { get; init; }

    public decimal NoShowRatePercent =>
        TotalAppointments == 0 ? 0m : Math.Round(100m * NoShows / TotalAppointments, 1);

    public decimal CancellationRatePercent =>
        TotalAppointments == 0 ? 0m : Math.Round(100m * Cancelled / TotalAppointments, 1);

    public int TotalBookedMinutes { get; init; }
    public int TotalAvailableMinutes { get; init; }
    public decimal UtilisationPercent =>
        TotalAvailableMinutes == 0 ? 0m : Math.Round(100m * TotalBookedMinutes / TotalAvailableMinutes, 1);

    public double AverageWaitMinutes { get; init; }
    public double AverageChairMinutes { get; init; }

    public IReadOnlyList<CategoryTotal> ByAppointmentType { get; init; } = Array.Empty<CategoryTotal>();
    public IReadOnlyList<ProviderProductionRow> ByProvider { get; init; } = Array.Empty<ProviderProductionRow>();
    public IReadOnlyList<DailySeriesPoint> DailyVolume { get; init; } = Array.Empty<DailySeriesPoint>();
}

/// <summary>Clinical activity and outcomes over a period.</summary>
public class ClinicalAnalytics
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }

    public int ProceduresCompleted { get; init; }
    public int SurgicalProcedures { get; init; }
    public int ImplantsPlaced { get; init; }
    public int ExtractionsPerformed { get; init; }
    public int RootCanalsCompleted { get; init; }
    public int RestorationsPlaced { get; init; }
    public int HygieneVisits { get; init; }
    public int RadiographsTaken { get; init; }
    public int PrescriptionsIssued { get; init; }

    public int ComplicationCount { get; init; }
    public decimal ComplicationRatePercent =>
        SurgicalProcedures == 0 ? 0m : Math.Round(100m * ComplicationCount / SurgicalProcedures, 2);

    public int RedoProcedures { get; init; }
    public int ReferralsOut { get; init; }

    public IReadOnlyList<CategoryTotal> ByProcedureCategory { get; init; } = Array.Empty<CategoryTotal>();
    public IReadOnlyList<CategoryTotal> TopProcedures { get; init; } = Array.Empty<CategoryTotal>();
    public IReadOnlyList<CategoryTotal> BySurgeryType { get; init; } = Array.Empty<CategoryTotal>();
}

/// <summary>One row of the treatment plan acceptance report.</summary>
public record TreatmentAcceptanceRow(
    Guid PlanId,
    string PlanNumber,
    Guid PatientId,
    string PatientName,
    string? ProviderName,
    DateOnly CreatedOn,
    DateOnly? PresentedOn,
    TreatmentPlanStatus Status,
    decimal TotalFee,
    decimal AcceptedValue)
{
    public decimal AcceptancePercent => TotalFee == 0m ? 0m : Math.Round(100m * AcceptedValue / TotalFee, 1);
    public int? DaysSincePresented =>
        PresentedOn is null ? null : DateOnly.FromDateTime(DateTime.Today).DayNumber - PresentedOn.Value.DayNumber;
}

/// <summary>One row of the recall worklist.</summary>
public record RecallDueRow(
    Guid RecallId,
    Guid PatientId,
    string PatientNumber,
    string PatientName,
    string? Phone,
    string? Email,
    RecallType RecallType,
    DateOnly DueDate,
    RecallStatus Status,
    int ContactAttempts,
    string? ProviderName)
{
    public int DaysOverdue
    {
        get
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            return DueDate >= today ? 0 : today.DayNumber - DueDate.DayNumber;
        }
    }

    public string UrgencyClass => DaysOverdue switch
    {
        > 180 => "text-danger fw-semibold",
        > 60 => "text-warning-emphasis",
        > 0 => "text-body-secondary",
        _ => ""
    };
}

/// <summary>One row of the outstanding-balance report.</summary>
public record ReceivableRow(
    Guid PatientId,
    string PatientNumber,
    string PatientName,
    string? Phone,
    decimal Balance,
    decimal Current,
    decimal Days1To30,
    decimal Days31To60,
    decimal Days61To90,
    decimal Over90,
    DateOnly? OldestDueDate,
    DateOnly? LastPaymentDate)
{
    public int? DaysSinceOldestDue =>
        OldestDueDate is null ? null : DateOnly.FromDateTime(DateTime.Today).DayNumber - OldestDueDate.Value.DayNumber;
}
