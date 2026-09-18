using System.Data;
using Microsoft.EntityFrameworkCore;
using WecErp.Application.Inventory;
using WecErp.Application.SalesOrders;
using WecErp.Domain;
using WecErp.Infrastructure;

public static class SalesOrderEndpoints
{
    public static void MapSalesOrderEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/sales-orders/{id:guid}/status", async (
            Guid id,
            ChangeSalesOrderStatusRequest request,
            SalesOrderService service,
            ErpDbContext db,
            CancellationToken ct) =>
        {
            var order = await db.SalesOrders.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (order is null) return Results.NotFound(new { error = "Sales order not found." });

            var error = String.Empty;
            if (!service.TryChangeStatus(order, request.Status, ref error))
                return Results.BadRequest(new { error });

            await db.SaveChangesAsync(ct);
            return Results.Ok(order);
        });

        app.MapPost("/api/v1/sales-orders/{id:guid}/fulfill", async (
            Guid id,
            FulfillSalesOrderRequest request,
            ErpDbContext db,
            CancellationToken ct) =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

            var order = await db.SalesOrders.Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (order is null) return Results.NotFound(new { error = "Sales order not found." });
            if (order.Status != SalesOrderStatus.Confirmed)
                return Results.BadRequest(new { error = "Only confirmed sales orders can be fulfilled." });

            var itemIds = order.Lines.Select(x => x.ItemId).Distinct().ToList();
            var items = await db.Items.Where(x => itemIds.Contains(x.Id) && x.IsActive)
                .ToDictionaryAsync(x => x.Id, ct);
            if (items.Count != itemIds.Count)
                return Results.BadRequest(new { error = "One or more sales order items do not exist or are inactive." });

            var stockLines = order.Lines.Where(x => items[x.ItemId].Type == ItemType.Product).ToList();
            if (stockLines.Count > 0)
            {
                if (!request.WarehouseId.HasValue || request.WarehouseId.Value == Guid.Empty)
                    return Results.BadRequest(new { error = "WarehouseId is required to fulfill product lines." });

                var warehouseExists = await db.Warehouses.AnyAsync(x => x.Id == request.WarehouseId.Value && x.IsActive, ct);
                if (!warehouseExists)
                    return Results.BadRequest(new { error = "Warehouse does not exist or is inactive." });

                foreach (var line in stockLines)
                {
                    var onHand = await CalculateOnHand(db, line.ItemId, request.WarehouseId.Value, ct);
                    var reserved = await CalculateReserved(db, line.ItemId, request.WarehouseId.Value, ct);
                    if (onHand - reserved < line.Quantity)
                        return Results.Conflict(new { error = $"Insufficient available stock for item {line.ItemId}." });
                }

                foreach (var line in stockLines)
                {
                    db.InventoryMovements.Add(new InventoryMovement
                    {
                        Id = Guid.NewGuid(),
                        ItemId = line.ItemId,
                        WarehouseId = request.WarehouseId.Value,
                        Type = InventoryMovementType.Issue,
                        Quantity = Decimal.Round(line.Quantity, 4, MidpointRounding.AwayFromZero),
                        ReferenceType = "SalesOrder",
                        ReferenceId = order.Id,
                        Notes = $"Fulfillment {order.Number}",
                        CreatedUtc = DateTimeOffset.UtcNow
                    });
                }
            }

            order.Status = SalesOrderStatus.Fulfilled;
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return Results.Ok(order);
        });
    }

    private static async Task<decimal> CalculateOnHand(ErpDbContext db, Guid itemId, Guid warehouseId, CancellationToken ct)
    {
        return await db.InventoryMovements
            .Where(x => x.ItemId == itemId && x.WarehouseId == warehouseId)
            .SumAsync(x =>
                x.Type == InventoryMovementType.Receipt ||
                x.Type == InventoryMovementType.TransferIn ||
                x.Type == InventoryMovementType.AdjustmentIncrease ? x.Quantity :
                x.Type == InventoryMovementType.Issue ||
                x.Type == InventoryMovementType.TransferOut ||
                x.Type == InventoryMovementType.AdjustmentDecrease ? -x.Quantity : 0m, ct);
    }

    private static async Task<decimal> CalculateReserved(ErpDbContext db, Guid itemId, Guid warehouseId, CancellationToken ct)
    {
        return await db.InventoryMovements
            .Where(x => x.ItemId == itemId && x.WarehouseId == warehouseId)
            .SumAsync(x =>
                x.Type == InventoryMovementType.Reservation ? x.Quantity :
                x.Type == InventoryMovementType.ReleaseReservation ? -x.Quantity : 0m, ct);
    }
}
