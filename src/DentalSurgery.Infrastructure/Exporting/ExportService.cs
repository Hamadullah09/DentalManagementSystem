using DentalSurgery.Application.Clinical;
using DentalSurgery.Application.Common;
using DentalSurgery.Application.Exporting;
using DentalSurgery.Domain.Enums;
using DentalSurgery.Infrastructure.Exporting.Documents;
using DentalSurgery.Infrastructure.Persistence;
using DentalSurgery.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;

namespace DentalSurgery.Infrastructure.Exporting;

/// <summary>A generated file, ready to be streamed to the browser.</summary>
public record ExportedFile(byte[] Content, string ContentType, string FileName)
{
    public int SizeBytes => Content.Length;
}

/// <summary>
/// Renders reports and clinical documents. Reports come from the catalogue and
/// go through a format-specific exporter; documents are purpose-built PDFs.
/// </summary>
public class ExportService(
    ReportCatalogue catalogue,
    IEnumerable<IReportExporter> exporters,
    IDbContextFactory<DentalDbContext> dbFactory,
    IPracticeAccessor practice,
    ClinicalService clinical,
    PatientService patients,
    ILogger<ExportService> logger)
{
    public IReadOnlyList<ReportDefinition> AvailableReports => ReportCatalogue.Definitions;

    // ---------------------------------------------------------------- reports

    public async Task<Result<ExportedFile>> ExportReportAsync(
        string reportKey, ExportFormat format, DateOnly? from, DateOnly? to, CancellationToken ct = default)
    {
        var exporter = exporters.FirstOrDefault(e => e.Format == format);
        if (exporter is null)
            return Result<ExportedFile>.Failure($"No exporter is registered for {format}.");

        var table = await catalogue.BuildAsync(reportKey, from, to, ct);
        if (table is null)
            return Result<ExportedFile>.Failure($"There is no report called '{reportKey}'.");

        try
        {
            var content = exporter.Render(table);
            var fileName = $"{table.FileStem}.{exporter.FileExtension}";

            logger.LogInformation("Exported {Report} as {Format} ({Rows} rows, {Bytes} bytes).",
                reportKey, format, table.Rows.Count, content.Length);

            return Result<ExportedFile>.Success(new ExportedFile(content, exporter.ContentType, fileName));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Rendering {Report} as {Format} failed.", reportKey, format);
            return Result<ExportedFile>.Failure($"The report could not be rendered: {ex.Message}");
        }
    }

    // ---------------------------------------------------------------- documents

    public async Task<Result<ExportedFile>> InvoicePdfAsync(Guid invoiceId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var invoice = await db.Invoices.AsNoTracking()
            .Include(i => i.Patient)
            .Include(i => i.GuarantorPatient)
            .Include(i => i.Provider)
            .Include(i => i.Location)
            .Include(i => i.Lines).ThenInclude(l => l.ProcedureCode)
            .Include(i => i.Lines).ThenInclude(l => l.Tooth)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);

        if (invoice is null) return Result<ExportedFile>.Failure("Invoice not found.");

        var payments = await db.PaymentAllocations.AsNoTracking()
            .Where(a => a.InvoiceId == invoiceId)
            .Include(a => a.Payment)
            .Select(a => a.Payment!)
            .ToListAsync(ct);

        var document = new InvoiceDocument(invoice, practice.Current, payments);
        return Pdf(document.GeneratePdf(), $"invoice-{Safe(invoice.InvoiceNumber)}.pdf");
    }

    public async Task<Result<ExportedFile>> TreatmentPlanPdfAsync(Guid planId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var plan = await db.TreatmentPlans.AsNoTracking()
            .Include(p => p.Patient)
            .Include(p => p.Provider)
            .Include(p => p.Phases).ThenInclude(ph => ph.Items).ThenInclude(i => i.ProcedureCode)
            .Include(p => p.Phases).ThenInclude(ph => ph.Items).ThenInclude(i => i.Tooth)
            .FirstOrDefaultAsync(p => p.Id == planId, ct);

        if (plan is null) return Result<ExportedFile>.Failure("Treatment plan not found.");

        var document = new TreatmentPlanDocument(plan, practice.Current);
        return Pdf(document.GeneratePdf(), $"treatment-plan-{Safe(plan.PlanNumber)}.pdf");
    }

    public async Task<Result<ExportedFile>> PrescriptionPdfAsync(Guid prescriptionId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var prescription = await db.Prescriptions.AsNoTracking()
            .Include(p => p.Patient)
            .Include(p => p.PrescriberStaff)
            .Include(p => p.Items).ThenInclude(i => i.Medication)
            .FirstOrDefaultAsync(p => p.Id == prescriptionId, ct);

        if (prescription is null) return Result<ExportedFile>.Failure("Prescription not found.");

        var document = new PrescriptionDocument(prescription, practice.Current);
        return Pdf(document.GeneratePdf(), $"prescription-{Safe(prescription.PrescriptionNumber)}.pdf");
    }

    public async Task<Result<ExportedFile>> ClaimFormPdfAsync(Guid claimId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var claim = await db.InsuranceClaims.AsNoTracking()
            .Include(c => c.Patient)
            .Include(c => c.Provider)
            .Include(c => c.PatientInsurance).ThenInclude(i => i!.InsurancePlan).ThenInclude(p => p!.InsuranceCarrier)
            .Include(c => c.Lines).ThenInclude(l => l.ProcedureCode)
            .Include(c => c.Lines).ThenInclude(l => l.Tooth)
            .FirstOrDefaultAsync(c => c.Id == claimId, ct);

        if (claim is null) return Result<ExportedFile>.Failure("Claim not found.");

        var document = new ClaimFormDocument(claim, practice.Current);
        return Pdf(document.GeneratePdf(), $"claim-{Safe(claim.ClaimNumber)}.pdf");
    }

    /// <summary>Builds the clinical summary, optionally framed as a referral letter.</summary>
    public async Task<Result<ExportedFile>> ClinicalSummaryPdfAsync(
        Guid patientId, Guid? referralId = null, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var patient = await db.Patients.AsNoTracking()
            .Include(p => p.PrimaryProvider)
            .FirstOrDefaultAsync(p => p.Id == patientId, ct);

        if (patient is null) return Result<ExportedFile>.Failure("Patient not found.");

        var referral = referralId is null
            ? null
            : await db.Referrals.AsNoTracking()
                .Include(r => r.InternalProvider)
                .FirstOrDefaultAsync(r => r.Id == referralId && r.PatientId == patientId, ct);

        if (referralId is not null && referral is null)
            return Result<ExportedFile>.Failure("Referral not found for this patient.");

        var allergies = await db.PatientAllergies.AsNoTracking().Include(a => a.Allergen)
            .Where(a => a.PatientId == patientId && a.IsActive)
            .OrderByDescending(a => a.Severity).ToListAsync(ct);

        var conditions = await db.PatientMedicalConditions.AsNoTracking().Include(c => c.MedicalCondition)
            .Where(c => c.PatientId == patientId)
            .OrderByDescending(c => c.Severity).ToListAsync(ct);

        var medications = await db.PatientMedications.AsNoTracking().Include(m => m.Medication)
            .Where(m => m.PatientId == patientId && m.IsCurrent).ToListAsync(ct);

        var procedures = await db.Procedures.AsNoTracking()
            .Include(p => p.ProcedureCode).Include(p => p.Tooth).Include(p => p.Provider)
            .Where(p => p.PatientId == patientId && p.Status == ProcedureStatus.Completed)
            .OrderByDescending(p => p.DateOfService).Take(30).ToListAsync(ct);

        var notes = await db.ClinicalNotes.AsNoTracking()
            .Include(n => n.Provider)
            .Where(n => n.PatientId == patientId)
            .OrderByDescending(n => n.NoteDateUtc).Take(8).ToListAsync(ct);

        var radiographs = await db.RadiographRecords.AsNoTracking()
            .Where(r => r.PatientId == patientId)
            .OrderByDescending(r => r.TakenAtUtc).Take(12).ToListAsync(ct);

        var perio = await db.PeriodontalCharts.AsNoTracking()
            .Include(c => c.Measurements).ThenInclude(m => m.Tooth)
            .Where(c => c.PatientId == patientId)
            .OrderByDescending(c => c.ExamDate).FirstOrDefaultAsync(ct);

        var chart = await clinical.GetChartAsync(patientId, Dentition.Permanent, null, ct);
        var risk = await patients.GetRiskProfileAsync(patientId, ct);

        PeriodontalSummary? perioSummary = null;
        if (perio is not null)
            perioSummary = await clinical.AnalysePerioAsync(perio, patient.AgeYears, false, false);

        var data = new ClinicalSummaryData
        {
            Patient = patient,
            Risk = risk,
            Allergies = allergies,
            Conditions = conditions,
            Medications = medications,
            Procedures = procedures,
            Notes = notes,
            Radiographs = radiographs,
            Chart = chart,
            Perio = perio,
            PerioSummary = perioSummary,
            Referral = referral
        };

        var document = new ClinicalSummaryDocument(data, practice.Current);
        var stem = referral is null
            ? $"clinical-summary-{Safe(patient.PatientNumber)}"
            : $"referral-{Safe(referral.ReferralNumber)}";

        return Pdf(document.GeneratePdf(), $"{stem}.pdf");
    }

    private static Result<ExportedFile> Pdf(byte[] content, string fileName) =>
        Result<ExportedFile>.Success(new ExportedFile(content, "application/pdf", fileName));

    private static string Safe(string value) =>
        new(value.Select(c => char.IsLetterOrDigit(c) || c == '-' ? char.ToLowerInvariant(c) : '-').ToArray());
}
