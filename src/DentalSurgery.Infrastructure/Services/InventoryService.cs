using DentalSurgery.Application.Abstractions;
using DentalSurgery.Application.Common;
using DentalSurgery.Domain.Entities;
using DentalSurgery.Domain.Enums;
using DentalSurgery.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalSurgery.Infrastructure.Services;

/// <summary>Stock, purchasing, sterilisation and instrument tracking.</summary>
public class InventoryService(
    DentalDbContext db,
    INumberSequenceService sequences,
    ICurrentUser currentUser,
    IDateTimeProvider clock,
    ILogger<InventoryService> logger,
    IPermissionGuard guard)
{
    // ------------------------------------------------------------------ stock

    public async Task<List<InventoryItem>> GetItemsAsync(
        InventoryCategory? category = null, bool onlyLowStock = false, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.InventoryView, ct);
        var query = db.InventoryItems.AsNoTracking()
            .Include(i => i.PreferredSupplier)
            .Where(i => i.IsActive);

        if (category.HasValue) query = query.Where(i => i.Category == category.Value);
        if (onlyLowStock) query = query.Where(i => i.CurrentStock <= i.ReorderLevel);

        return await query.OrderBy(i => i.Category).ThenBy(i => i.Name).ToListAsync(ct);
    }

    public async Task<InventoryItem?> GetItemAsync(Guid id, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.InventoryView, ct);
        return await db.InventoryItems
            .Include(i => i.PreferredSupplier)
            .Include(i => i.Lots.OrderBy(l => l.ExpiryDate))
            .FirstOrDefaultAsync(i => i.Id == id, ct);
    }

    public async Task<List<StockMovement>> GetMovementsAsync(
        Guid itemId, int take = 100, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.InventoryView, ct);
        return await db.StockMovements.AsNoTracking()
            .Include(m => m.InventoryLot)
            .Where(m => m.InventoryItemId == itemId)
            .OrderByDescending(m => m.MovementDateUtc)
            .Take(take)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Issues stock against a procedure, drawing from the lot that expires first
    /// so short-dated material is used before it goes out of date.
    /// </summary>
    public async Task<Result<StockMovement>> ConsumeAsync(
        Guid itemId, decimal quantity, Guid? procedureId = null,
        Guid? lotId = null, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.InventoryAdjust, ct);
        if (quantity <= 0) return Result<StockMovement>.Failure("The quantity must be greater than zero.");

        var item = await db.InventoryItems
            .Include(i => i.Lots)
            .FirstOrDefaultAsync(i => i.Id == itemId, ct);

        if (item is null) return Result<StockMovement>.Failure("Inventory item not found.");

        InventoryLot? lot = null;
        if (item.RequiresLotTracking)
        {
            lot = lotId.HasValue
                ? item.Lots.FirstOrDefault(l => l.Id == lotId)
                : item.Lots
                    .Where(l => l.QuantityRemaining > 0 && !l.IsQuarantined && !l.IsExpired)
                    .OrderBy(l => l.ExpiryDate ?? DateOnly.MaxValue)
                    .FirstOrDefault();

            if (lot is null)
                return Result<StockMovement>.Failure($"No usable lot of {item.Name} is in stock.");

            if (lot.QuantityRemaining < quantity)
                return Result<StockMovement>.Failure(
                    $"Lot {lot.LotNumber} has only {lot.QuantityRemaining} {item.UnitOfMeasure} remaining.");

            lot.QuantityRemaining -= quantity;
        }

        item.CurrentStock -= quantity;

        var movement = new StockMovement
        {
            InventoryItemId = itemId,
            InventoryLotId = lot?.Id,
            MovementDateUtc = clock.UtcNow,
            MovementType = StockMovementType.Issue,
            Quantity = -quantity,
            BalanceAfter = item.CurrentStock,
            UnitCost = lot?.UnitCost ?? item.UnitCost,
            ProcedureId = procedureId,
            PerformedByStaffId = currentUser.StaffId,
            Reference = procedureId?.ToString()
        };

        db.StockMovements.Add(movement);
        await db.SaveChangesAsync(ct);

        if (item.IsBelowReorderLevel)
        {
            logger.LogWarning("{Item} has fallen to {Stock} {Unit}, at or below the reorder level of {Level}.",
                item.Name, item.CurrentStock, item.UnitOfMeasure, item.ReorderLevel);
        }

        return Result<StockMovement>.Success(movement);
    }

    public async Task<Result<StockMovement>> AdjustAsync(
        Guid itemId, decimal signedQuantity, StockMovementType type,
        string reason, Guid? lotId = null, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.InventoryAdjust, ct);
        var item = await db.InventoryItems.Include(i => i.Lots).FirstOrDefaultAsync(i => i.Id == itemId, ct);
        if (item is null) return Result<StockMovement>.Failure("Inventory item not found.");
        if (signedQuantity == 0) return Result<StockMovement>.Failure("The adjustment cannot be zero.");

        var lot = lotId.HasValue ? item.Lots.FirstOrDefault(l => l.Id == lotId) : null;
        if (lot is not null) lot.QuantityRemaining = Math.Max(0m, lot.QuantityRemaining + signedQuantity);

        item.CurrentStock += signedQuantity;
        if (item.CurrentStock < 0) item.CurrentStock = 0;

        var movement = new StockMovement
        {
            InventoryItemId = itemId,
            InventoryLotId = lot?.Id,
            MovementDateUtc = clock.UtcNow,
            MovementType = type,
            Quantity = signedQuantity,
            BalanceAfter = item.CurrentStock,
            UnitCost = lot?.UnitCost ?? item.UnitCost,
            Reason = reason,
            PerformedByStaffId = currentUser.StaffId
        };

        db.StockMovements.Add(movement);
        await db.SaveChangesAsync(ct);
        return Result<StockMovement>.Success(movement);
    }

    /// <summary>Writes off every lot that has passed its expiry date.</summary>
    public async Task<int> WriteOffExpiredLotsAsync(CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.InventoryAdjust, ct);

        var today = clock.Today;
        var expired = await db.InventoryLots
            .Include(l => l.InventoryItem)
            .Where(l => l.QuantityRemaining > 0 && l.ExpiryDate != null && l.ExpiryDate < today)
            .ToListAsync(ct);

        foreach (var lot in expired)
        {
            var quantity = lot.QuantityRemaining;
            lot.QuantityRemaining = 0;

            if (lot.InventoryItem is not null)
            {
                lot.InventoryItem.CurrentStock = Math.Max(0m, lot.InventoryItem.CurrentStock - quantity);

                db.StockMovements.Add(new StockMovement
                {
                    InventoryItemId = lot.InventoryItemId,
                    InventoryLotId = lot.Id,
                    MovementDateUtc = clock.UtcNow,
                    MovementType = StockMovementType.Expiry,
                    Quantity = -quantity,
                    BalanceAfter = lot.InventoryItem.CurrentStock,
                    UnitCost = lot.UnitCost,
                    Reason = $"Lot {lot.LotNumber} expired on {lot.ExpiryDate:d MMM yyyy}."
                });
            }
        }

        if (expired.Count > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Wrote off {Count} expired lots.", expired.Count);
        }

        return expired.Count;
    }

    public async Task<List<InventoryLot>> GetExpiringLotsAsync(int withinDays = 90, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.InventoryView, ct);
        var cutoff = clock.Today.AddDays(withinDays);
        return await db.InventoryLots.AsNoTracking()
            .Include(l => l.InventoryItem)
            .Where(l => l.QuantityRemaining > 0 && l.ExpiryDate != null && l.ExpiryDate <= cutoff)
            .OrderBy(l => l.ExpiryDate)
            .ToListAsync(ct);
    }

    // ------------------------------------------------------------------ purchasing

    public async Task<Result<PurchaseOrder>> CreatePurchaseOrderAsync(
        Guid supplierId, IEnumerable<(Guid ItemId, decimal Quantity)> lines, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.InventoryCreate, ct);
        var supplier = await db.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == supplierId, ct);
        if (supplier is null) return Result<PurchaseOrder>.Failure("Supplier not found.");

        var order = new PurchaseOrder
        {
            SupplierId = supplierId,
            OrderNumber = await sequences.NextAsync(SequenceNames.PurchaseOrder, ct),
            OrderDate = clock.Today,
            ExpectedDate = clock.Today.AddDays(supplier.LeadTimeDays),
            Status = PurchaseOrderStatus.Draft,
            OrderedBy = currentUser.DisplayName ?? currentUser.UserName
        };

        var sequence = 1;
        foreach (var (itemId, quantity) in lines)
        {
            var item = await db.InventoryItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == itemId, ct);
            if (item is null) continue;

            order.Lines.Add(new PurchaseOrderLine
            {
                PurchaseOrderId = order.Id,
                InventoryItemId = itemId,
                Sequence = sequence++,
                QuantityOrdered = quantity,
                UnitCost = item.LastPurchasePrice ?? item.UnitCost
            });
        }

        if (order.Lines.Count == 0) return Result<PurchaseOrder>.Failure("The order has no valid lines.");

        order.Subtotal = order.Lines.Sum(l => l.LineTotal);
        order.Total = order.Subtotal + order.TaxAmount + order.ShippingCost;

        db.PurchaseOrders.Add(order);
        await db.SaveChangesAsync(ct);
        return Result<PurchaseOrder>.Success(order);
    }

    /// <summary>Builds a draft order for everything currently below its reorder level.</summary>
    public async Task<IReadOnlyList<PurchaseOrder>> GenerateReorderDraftsAsync(CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.InventoryCreate, ct);

        var low = await db.InventoryItems.AsNoTracking()
            .Where(i => i.IsActive && i.CurrentStock <= i.ReorderLevel && i.PreferredSupplierId != null)
            .ToListAsync(ct);

        var orders = new List<PurchaseOrder>();

        foreach (var group in low.GroupBy(i => i.PreferredSupplierId!.Value))
        {
            var lines = group.Select(i => (i.Id, i.ReorderQuantity > 0 ? i.ReorderQuantity : 1m));
            var result = await CreatePurchaseOrderAsync(group.Key, lines, ct);
            if (result.Succeeded && result.Value is not null) orders.Add(result.Value);
        }

        logger.LogInformation("Generated {Count} reorder drafts covering {Items} items.", orders.Count, low.Count);
        return orders;
    }

    /// <summary>Books goods in, creating lots and raising stock.</summary>
    public async Task<Result> ReceiveOrderAsync(
        Guid orderId,
        IEnumerable<(Guid LineId, decimal Quantity, string? LotNumber, DateOnly? Expiry)> receipts,
        CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.InventoryAdjust, ct);
        var order = await db.PurchaseOrders
            .Include(o => o.Lines).ThenInclude(l => l.InventoryItem)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null) return Result.Failure("Purchase order not found.");
        if (order.Status == PurchaseOrderStatus.Cancelled) return Result.Failure("This order was cancelled.");

        foreach (var (lineId, quantity, lotNumber, expiry) in receipts)
        {
            var line = order.Lines.FirstOrDefault(l => l.Id == lineId);
            if (line?.InventoryItem is null || quantity <= 0) continue;

            var item = line.InventoryItem;
            line.QuantityReceived += quantity;

            InventoryLot? lot = null;
            if (item.RequiresLotTracking || !string.IsNullOrWhiteSpace(lotNumber))
            {
                lot = new InventoryLot
                {
                    InventoryItemId = item.Id,
                    LotNumber = lotNumber ?? $"AUTO-{clock.Today:yyyyMMdd}",
                    ExpiryDate = expiry,
                    ReceivedDate = clock.Today,
                    QuantityReceived = quantity,
                    QuantityRemaining = quantity,
                    UnitCost = line.UnitCost,
                    PurchaseOrderId = order.Id,
                    SupplierId = order.SupplierId
                };
                db.InventoryLots.Add(lot);
            }

            item.CurrentStock += quantity;
            item.LastPurchasePrice = line.UnitCost;
            item.LastPurchaseDate = clock.Today;

            db.StockMovements.Add(new StockMovement
            {
                InventoryItemId = item.Id,
                InventoryLotId = lot?.Id,
                MovementDateUtc = clock.UtcNow,
                MovementType = StockMovementType.Receipt,
                Quantity = quantity,
                BalanceAfter = item.CurrentStock,
                UnitCost = line.UnitCost,
                PurchaseOrderId = order.Id,
                Reference = order.OrderNumber,
                PerformedByStaffId = currentUser.StaffId
            });
        }

        order.Status = order.IsFullyReceived ? PurchaseOrderStatus.Received : PurchaseOrderStatus.PartiallyReceived;
        if (order.Status == PurchaseOrderStatus.Received) order.ReceivedDate = clock.Today;

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    // ------------------------------------------------------------------ sterilisation

    public async Task<List<SterilisationCycle>> GetCyclesAsync(
        DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.SterilizationView, ct);
        var start = from.ToDateTime(TimeOnly.MinValue);
        var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue);

        return await db.SterilisationCycles.AsNoTracking()
            .Include(c => c.Steriliser).Include(c => c.OperatorStaff)
            .Where(c => c.StartedAtUtc >= start && c.StartedAtUtc < end)
            .OrderByDescending(c => c.StartedAtUtc)
            .ToListAsync(ct);
    }

    public async Task<Result<SterilisationCycle>> RecordCycleAsync(
        SterilisationCycle cycle, IEnumerable<Guid>? instrumentSetIds = null, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.SterilizationRecordCycle, ct);
        var steriliser = await db.Sterilisers.FirstOrDefaultAsync(s => s.Id == cycle.SteriliserId, ct);
        if (steriliser is null) return Result<SterilisationCycle>.Failure("Steriliser not found.");

        cycle.CycleNumber = steriliser.NextCycleNumber;
        steriliser.NextCycleNumber++;
        cycle.OperatorStaffId ??= currentUser.StaffId;

        // A failed indicator quarantines the load: nothing from it may be used.
        var passed = cycle.ChemicalIndicatorPass && cycle.HelixTestPass &&
                     cycle.VacuumLeakTestPass && cycle.BiologicalIndicatorPass != false;

        cycle.Result = passed ? SterilisationResult.Pass : SterilisationResult.Fail;
        cycle.CompletedAtUtc ??= clock.UtcNow;

        db.SterilisationCycles.Add(cycle);

        var setIds = instrumentSetIds?.ToList() ?? new List<Guid>();
        if (setIds.Count > 0)
        {
            var sets = await db.InstrumentSets.Where(s => setIds.Contains(s.Id)).ToListAsync(ct);
            foreach (var set in sets)
            {
                set.LastCycleId = cycle.Id;
                set.SterilisedOn = clock.Today;

                if (passed)
                {
                    set.Status = InstrumentSetStatus.Sterile;
                    // Pouched instruments are treated as sterile for 12 months.
                    set.SterilityExpiryDate = clock.Today.AddMonths(12);
                }
                else
                {
                    set.Status = InstrumentSetStatus.Quarantined;
                    set.SterilityExpiryDate = null;
                }
            }
            cycle.ItemCount = sets.Sum(s => s.ItemCount);
        }

        await db.SaveChangesAsync(ct);

        if (!passed)
        {
            logger.LogError(
                "Sterilisation cycle {Number} on {Steriliser} FAILED. {Count} instrument sets quarantined.",
                cycle.CycleNumber, steriliser.Name, setIds.Count);
        }

        return Result<SterilisationCycle>.Success(cycle);
    }

    public async Task<Result> RecordInstrumentUseAsync(
        Guid setId, Guid? procedureId, Guid? patientId, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.SterilizationRecordCycle, ct);
        var set = await db.InstrumentSets.FirstOrDefaultAsync(s => s.Id == setId, ct);
        if (set is null) return Result.Failure("Instrument set not found.");
        if (set.Status != InstrumentSetStatus.Sterile)
            return Result.Failure($"Set {set.SetCode} is marked {set.Status} and must not be used.");
        if (set.IsExpired)
            return Result.Failure($"Set {set.SetCode} passed its sterility expiry on {set.SterilityExpiryDate:d MMM yyyy}.");

        db.InstrumentSetUsages.Add(new InstrumentSetUsage
        {
            InstrumentSetId = setId,
            ProcedureId = procedureId,
            PatientId = patientId,
            UsedAtUtc = clock.UtcNow,
            UsedByStaffId = currentUser.StaffId,
            CycleIdAtTimeOfUse = set.LastCycleId
        });

        set.Status = InstrumentSetStatus.Contaminated;
        set.UsageCount++;
        set.LastUsedOn = clock.Today;

        await db.SaveChangesAsync(ct);
        return Result.Success();
    }

    /// <summary>
    /// Traces which patients were treated with instruments from a given cycle.
    /// Used when a cycle failure is discovered after the fact.
    /// </summary>
    public async Task<List<InstrumentSetUsage>> TraceCycleAsync(Guid cycleId, CancellationToken ct = default)
    {
        await guard.DemandAsync(Permissions.SterilizationView, ct);
        return await db.InstrumentSetUsages.AsNoTracking()
            .Include(u => u.InstrumentSet)
            .Where(u => u.CycleIdAtTimeOfUse == cycleId)
            .OrderBy(u => u.UsedAtUtc)
            .ToListAsync(ct);
    }
}
