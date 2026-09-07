using DentalSurgery.Application.Common;
using DentalSurgery.Domain.Entities;
using DentalSurgery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace DentalSurgery.Infrastructure.Services;

/// <summary>One parsed row of a fee import, with what it will do.</summary>
public record FeeImportRow(
    int LineNumber,
    string Code,
    string? Description,
    decimal? Fee,
    decimal? AllowedAmount,
    decimal? CoveragePercent,
    decimal? CurrentFee,
    string Outcome,
    string? Problem = null)
{
    public bool IsValid => Problem is null;
    public bool IsChange => IsValid && CurrentFee != Fee;
}

public record FeeImportPreview(
    IReadOnlyList<FeeImportRow> Rows,
    int Valid,
    int Invalid,
    int Changes,
    int Unmatched)
{
    public bool CanApply => Valid > 0;
}

/// <summary>
/// Imports a price list into a fee schedule from CSV. Every practice sets its
/// own fees and insurers issue their own allowed amounts, so this replaces
/// re-typing 130 codes by hand.
///
/// Expected header: Code, Fee, and optionally Allowed, Coverage, Description.
/// Column order does not matter; the header row is used to locate them.
/// </summary>
public class FeeScheduleImporter(
    IDbContextFactory<DentalDbContext> dbFactory,
    ILogger<FeeScheduleImporter> logger)
{
    /// <summary>Parses and validates the file without writing anything.</summary>
    public async Task<Result<FeeImportPreview>> PreviewAsync(
        Guid feeScheduleId, string csv, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(csv))
            return Result<FeeImportPreview>.Failure("The file is empty.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var schedule = await db.FeeSchedules.AsNoTracking()
            .Include(f => f.Items)
            .FirstOrDefaultAsync(f => f.Id == feeScheduleId, ct);

        if (schedule is null) return Result<FeeImportPreview>.Failure("Fee schedule not found.");

        var codes = await db.ProcedureCodes.AsNoTracking()
            .ToDictionaryAsync(c => c.Code.ToUpperInvariant(), c => c, ct);

        var existing = schedule.Items.ToDictionary(i => i.ProcedureCodeId, i => i.Fee);

        var lines = csv.Replace("\r\n", "\n").Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        if (lines.Count < 2)
            return Result<FeeImportPreview>.Failure("The file needs a header row and at least one fee.");

        var header = ParseLine(lines[0]).Select(h => h.Trim().ToLowerInvariant()).ToList();

        var codeIndex = FindColumn(header, "code", "procedure", "procedurecode");
        var feeIndex = FindColumn(header, "fee", "price", "amount", "charge");
        var allowedIndex = FindColumn(header, "allowed", "allowedamount", "payerallowed");
        var coverageIndex = FindColumn(header, "coverage", "coveragepercent", "percent");
        var descriptionIndex = FindColumn(header, "description", "name");

        if (codeIndex < 0)
            return Result<FeeImportPreview>.Failure(
                "No procedure code column was found. Name it 'Code'.");
        if (feeIndex < 0)
            return Result<FeeImportPreview>.Failure(
                "No fee column was found. Name it 'Fee'.");

        var rows = new List<FeeImportRow>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var line = 1; line < lines.Count; line++)
        {
            var cells = ParseLine(lines[line]);
            var lineNumber = line + 1;

            var code = Get(cells, codeIndex)?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(code))
            {
                rows.Add(new FeeImportRow(lineNumber, "", null, null, null, null, null,
                    "Skipped", "No procedure code."));
                continue;
            }

            if (!seen.Add(code))
            {
                rows.Add(new FeeImportRow(lineNumber, code, null, null, null, null, null,
                    "Skipped", "The code appears more than once in the file."));
                continue;
            }

            if (!codes.TryGetValue(code, out var procedureCode))
            {
                rows.Add(new FeeImportRow(lineNumber, code, null, null, null, null, null,
                    "Skipped", "No procedure with this code exists in the catalogue."));
                continue;
            }

            var fee = ParseMoney(Get(cells, feeIndex));
            if (fee is null)
            {
                rows.Add(new FeeImportRow(lineNumber, code, procedureCode.ShortDescription, null, null, null, null,
                    "Skipped", $"'{Get(cells, feeIndex)}' is not a valid fee."));
                continue;
            }

            if (fee < 0)
            {
                rows.Add(new FeeImportRow(lineNumber, code, procedureCode.ShortDescription, fee, null, null, null,
                    "Skipped", "The fee cannot be negative."));
                continue;
            }

            var coverage = ParseMoney(Get(cells, coverageIndex));
            if (coverage is > 100)
            {
                rows.Add(new FeeImportRow(lineNumber, code, procedureCode.ShortDescription, fee, null, coverage, null,
                    "Skipped", "Coverage cannot exceed 100%."));
                continue;
            }

            var current = existing.TryGetValue(procedureCode.Id, out var value) ? value : (decimal?)null;

            rows.Add(new FeeImportRow(
                lineNumber, code,
                Get(cells, descriptionIndex) ?? procedureCode.ShortDescription,
                fee,
                ParseMoney(Get(cells, allowedIndex)),
                coverage,
                current,
                current is null ? "Add" : current == fee ? "Unchanged" : "Update"));
        }

        var valid = rows.Count(r => r.IsValid);

        return Result<FeeImportPreview>.Success(new FeeImportPreview(
            rows,
            valid,
            rows.Count - valid,
            rows.Count(r => r.IsChange),
            rows.Count(r => r.Problem?.Contains("catalogue") == true)));
    }

    /// <summary>Writes the valid rows into the schedule.</summary>
    public async Task<Result<int>> ApplyAsync(
        Guid feeScheduleId, string csv, CancellationToken ct = default)
    {
        var preview = await PreviewAsync(feeScheduleId, csv, ct);
        if (preview.Failed) return Result<int>.Failure(preview.Errors);
        if (!preview.Value!.CanApply) return Result<int>.Failure("There are no valid rows to import.");

        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var schedule = await db.FeeSchedules
            .Include(f => f.Items)
            .FirstOrDefaultAsync(f => f.Id == feeScheduleId, ct);

        if (schedule is null) return Result<int>.Failure("Fee schedule not found.");

        var codes = await db.ProcedureCodes.AsNoTracking()
            .ToDictionaryAsync(c => c.Code.ToUpperInvariant(), c => c.Id, ct);

        var applied = 0;

        foreach (var row in preview.Value.Rows.Where(r => r.IsValid && r.Fee.HasValue))
        {
            if (!codes.TryGetValue(row.Code, out var procedureCodeId)) continue;

            // Guaranteed by the Where clause above, which flow analysis cannot see through.
            var fee = row.Fee!.Value;

            var item = schedule.Items.FirstOrDefault(i => i.ProcedureCodeId == procedureCodeId);

            if (item is null)
            {
                // Added through the set, not through schedule.Items: the entity carries
                // its own key from the moment it is constructed, so change detection
                // would take it for an existing row and issue an UPDATE that matches nothing.
                db.FeeScheduleItems.Add(new FeeScheduleItem
                {
                    FeeScheduleId = schedule.Id,
                    ProcedureCodeId = procedureCodeId,
                    Fee = fee,
                    AllowedAmount = row.AllowedAmount,
                    CoveragePercent = row.CoveragePercent,
                    Notes = "Imported"
                });
            }
            else
            {
                item.Fee = fee;
                if (row.AllowedAmount.HasValue) item.AllowedAmount = row.AllowedAmount;
                if (row.CoveragePercent.HasValue) item.CoveragePercent = row.CoveragePercent;
            }

            applied++;
        }

        await db.SaveChangesAsync(ct);

        logger.LogInformation("Imported {Count} fees into schedule {Schedule}.", applied, schedule.Name);
        return Result<int>.Success(applied);
    }

    /// <summary>Exports the current schedule in the format the importer accepts.</summary>
    public async Task<string> ExportTemplateAsync(Guid feeScheduleId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var items = await db.FeeScheduleItems.AsNoTracking()
            .Include(i => i.ProcedureCode)
            .Where(i => i.FeeScheduleId == feeScheduleId)
            .ToDictionaryAsync(i => i.ProcedureCodeId, i => i, ct);

        var codes = await db.ProcedureCodes.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Code)
            .ToListAsync(ct);

        var builder = new System.Text.StringBuilder();
        builder.AppendLine("Code,Description,Fee,Allowed,Coverage");

        foreach (var code in codes)
        {
            var item = items.GetValueOrDefault(code.Id);
            var fee = item?.Fee ?? code.DefaultFee;

            builder.AppendLine(string.Join(",",
                code.Code,
                Quote(code.ShortDescription),
                fee.ToString("0.00", CultureInfo.InvariantCulture),
                item?.AllowedAmount?.ToString("0.00", CultureInfo.InvariantCulture) ?? "",
                item?.CoveragePercent?.ToString("0.#", CultureInfo.InvariantCulture) ?? ""));
        }

        return builder.ToString();
    }

    // ---------------------------------------------------------------- parsing

    private static int FindColumn(List<string> header, params string[] candidates)
    {
        for (var i = 0; i < header.Count; i++)
        {
            var normalised = header[i].Replace(" ", "").Replace("_", "");
            if (candidates.Any(c => normalised.Equals(c, StringComparison.OrdinalIgnoreCase))) return i;
        }
        return -1;
    }

    private static string? Get(IReadOnlyList<string> cells, int index) =>
        index >= 0 && index < cells.Count && !string.IsNullOrWhiteSpace(cells[index]) ? cells[index] : null;

    /// <summary>Accepts currency symbols, thousands separators and blank cells.</summary>
    private static decimal? ParseMoney(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var cleaned = new string(value.Where(c => char.IsDigit(c) || c is '.' or '-' or ',').ToArray())
            .Replace(",", "");

        return decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? Math.Round(parsed, 2)
            : null;
    }

    /// <summary>Minimal RFC 4180 reader: handles quoted fields and doubled quotes.</summary>
    private static List<string> ParseLine(string line)
    {
        var cells = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var character = line[i];

            if (inQuotes)
            {
                if (character == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                    else inQuotes = false;
                }
                else current.Append(character);
            }
            else if (character == '"') inQuotes = true;
            else if (character == ',') { cells.Add(current.ToString()); current.Clear(); }
            else current.Append(character);
        }

        cells.Add(current.ToString());
        return cells;
    }

    private static string Quote(string value) =>
        value.Contains(',') || value.Contains('"') ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
}
