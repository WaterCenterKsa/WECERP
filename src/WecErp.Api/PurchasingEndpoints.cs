using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.EntityFrameworkCore;
using WecErp.Application.Inventory;
using WecErp.Application.Purchasing;
using WecErp.Application.Suppliers;
using WecErp.Domain;
using WecErp.Infrastructure;

public static class PurchasingEndpoints
{
    public static void MapPurchasingEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/suppliers", async (ErpDbContext db, int? page, int? pageSize, string? search, CancellationToken ct) =>
        {
            var currentPage = Math.Max(page ?? 1, 1);
            var size = Math.Clamp(pageSize ?? 50, 1, 200);
            var query = db.Suppliers.AsNoTracking().Where(x => x.IsActive);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x => x.Code.Contains(term) || x.Name.Contains(term) || x.Phone.Contains(term));
            }
            var total = await query.CountAsync(ct);
            var suppliers = await query.OrderBy(x => x.Name).ThenBy(x => x.Code)
                .Skip((currentPage - 1) * size).Take(size).ToListAsync(ct);
            return Results.Ok(new { page = currentPage, pageSize = size, total, suppliers });
        });

        app.MapPost("/api/v1/suppliers", async (CreateSupplierRequest request, [FromServices] SupplierService service, ErpDbContext db, CancellationToken ct) =>
        {
            var error = service.ValidateNewSupplier(request);
            if (!string.IsNullOrEmpty(error)) return Results.BadRequest(new { error });
            var code = request.Code.Trim();
            if (await db.Suppliers.AnyAsync(x => x.Code == code, ct))
                return Results.Conflict(new { error = "A supplier with this code already exists." });
            var supplier = service.CreateEntity(request);
            db.Suppliers.Add(supplier);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/suppliers/{supplier.Id}", service.ToDto(supplier));
        });

        app.MapGet("/api/v1/purchase-orders", async (ErpDbContext db, int? page, int? pageSize, string? search, CancellationToken ct) =>
        {
            var currentPage = Math.Max(page ?? 1, 1);
            var size = Math.Clamp(pageSize ?? 50, 1, 200);
            var query = db.PurchaseOrders.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x => x.Number.Contains(term));
            }
            var total = await query.CountAsync(ct);
            var orders = await query.OrderByDescending(x => x.CreatedUtc)
                .Skip((currentPage - 1) * size).Take(size)
                .Select(x => new { x.Id, x.Number, x.SupplierId, x.Status, x.CurrencyCode, x.Subtotal, x.TaxAmount, x.Total, x.CreatedUtc })
                .ToListAsync(ct);
            return Results.Ok(new { page = currentPage, pageSize = size, total, purchaseOrders = orders });
        });

        app.MapGet("/api/v1/purchase-orders/{id:guid}", async (Guid id, ErpDbContext db, CancellationToken ct) =>
        {
            var order = await db.PurchaseOrders.AsNoTracking().Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            return order is null ? Results.NotFound(new { error = "Purchase order not found." }) : Results.Ok(order);
        });

        app.MapPost("/api/v1/purchase-orders", async (CreatePurchaseOrderRequest request, [FromServices] PurchaseOrderService service, ErpDbContext db, CancellationToken ct) =>
        {
            var error = service.ValidateNewOrder(request);
            if (!string.IsNullOrEmpty(error)) return Results.BadRequest(new { error });

            var supplierExists = await db.Suppliers.AnyAsync(x => x.Id == request.SupplierId && x.IsActive, ct);
            if (!supplierExists) return Results.BadRequest(new { error = "Supplier does not exist or is inactive." });

            var ids = request.Lines.Select(x => x.ItemId).Distinct().ToList();
            var items = await db.Items.Where(x => ids.Contains(x.Id) && x.IsActive).ToDictionaryAsync(x => x.Id, ct);
            if (items.Count != ids.Count) return Results.BadRequest(new { error = "One or more purchase order items do not exist or are inactive." });

            var order = service.CreateEntity(request, items);
            db.PurchaseOrders.Add(order);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/purchase-orders/{order.Id}", order);
        });

        app.MapPost("/api/v1/purchase-orders/{id:guid}/status", async (Guid id, ChangePurchaseOrderStatusRequest request, [FromServices] PurchaseOrderService service, ErpDbContext db, CancellationToken ct) =>
        {
            var order = await db.PurchaseOrders.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (order is null) return Results.NotFound(new { error = "Purchase order not found." });
            var error = String.Empty;
            if (!service.TryChangeStatus(order, request.Status, ref error))
                return Results.BadRequest(new { error });
            await db.SaveChangesAsync(ct);
            return Results.Ok(order);
        });

        app.MapPost("/api/v1/purchase-orders/{id:guid}/receive", async (
            Guid id,
            ReceivePurchaseOrderRequest request,
            ErpDbContext db,
            InventoryService inventoryService,
            CancellationToken ct) =>
        {
            if (request.WarehouseId == Guid.Empty) return Results.BadRequest(new { error = "WarehouseId is required." });
            if (request.Lines is null || request.Lines.Count == 0) return Results.BadRequest(new { error = "At least one receipt line is required." });
            if (request.Lines.Any(x => x.PurchaseOrderLineId == Guid.Empty || x.Quantity <= 0m))
                return Results.BadRequest(new { error = "Each receipt line requires a valid line ID and a quantity greater than zero." });

            if (request.Lines.GroupBy(x => x.PurchaseOrderLineId).Any(g => g.Count() > 1))
                return Results.BadRequest(new { error = "Each purchase order line may appear only once per receipt." });

            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

            var order = await db.PurchaseOrders.Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (order is null) return Results.NotFound(new { error = "Purchase order not found." });
            if (order.Status != PurchaseOrderStatus.Confirmed && order.Status != PurchaseOrderStatus.PartiallyReceived)
                return Results.BadRequest(new { error = "Only confirmed or partially received purchase orders can be received." });

            var warehouse = await db.Warehouses.FirstOrDefaultAsync(x => x.Id == request.WarehouseId && x.IsActive, ct);
            if (warehouse is null) return Results.BadRequest(new { error = "Warehouse does not exist or is inactive." });

            var requestedLineIds = request.Lines.Select(x => x.PurchaseOrderLineId).ToList();
            var existingReceiptQty = await db.PurchaseReceiptLines
                .Where(x => requestedLineIds.Contains(x.PurchaseOrderLineId))
                .GroupBy(x => x.PurchaseOrderLineId)
                .Select(g => new { LineId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .ToDictionaryAsync(x => x.LineId, x => x.Quantity, ct);

            var receipt = new PurchaseReceipt
            {
                Id = Guid.NewGuid(),
                Number = $"GR-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant()}",
                PurchaseOrderId = order.Id,
                WarehouseId = warehouse.Id,
                CreatedUtc = DateTimeOffset.UtcNow
            };

            foreach (var requestLine in request.Lines)
            {
                var orderLine = order.Lines.FirstOrDefault(x => x.Id == requestLine.PurchaseOrderLineId);
                if (orderLine is null) return Results.BadRequest(new { error = "A receipt line does not belong to this purchase order." });

                var alreadyReceived = existingReceiptQty.TryGetValue(orderLine.Id, out var received) ? received : 0m;
                if (alreadyReceived + requestLine.Quantity > orderLine.OrderedQuantity)
                    return Results.Conflict(new { error = $"Receipt quantity exceeds ordered quantity for line {orderLine.Id}." });

                receipt.Lines.Add(new PurchaseReceiptLine
                {
                    Id = Guid.NewGuid(),
                    PurchaseReceiptId = receipt.Id,
                    PurchaseOrderLineId = orderLine.Id,
                    ItemId = orderLine.ItemId,
                    Quantity = Decimal.Round(requestLine.Quantity, 4, MidpointRounding.AwayFromZero)
                });

                var movementRequest = new CreateInventoryMovementRequest
                {
                    ItemId = orderLine.ItemId,
                    WarehouseId = warehouse.Id,
                    Type = InventoryMovementType.Receipt,
                    Quantity = requestLine.Quantity,
                    ReferenceType = "PurchaseReceipt",
                    ReferenceId = receipt.Id,
                    Notes = $"Receipt {receipt.Number}"
                };
                db.InventoryMovements.Add(inventoryService.CreateMovement(movementRequest, InventoryMovementType.Receipt, warehouse.Id));
            }

            db.PurchaseReceipts.Add(receipt);
            await db.SaveChangesAsync(ct);

            var allOrderLineIds = order.Lines.Select(x => x.Id).ToList();
            var receivedByLine = await db.PurchaseReceiptLines
                .Where(x => allOrderLineIds.Contains(x.PurchaseOrderLineId))
                .GroupBy(x => x.PurchaseOrderLineId)
                .Select(g => new { LineId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .ToDictionaryAsync(x => x.LineId, x => x.Quantity, ct);

            var fullyReceived = order.Lines.All(x => receivedByLine.TryGetValue(x.Id, out var received) && received >= x.OrderedQuantity);
            var partiallyReceived = order.Lines.Any(x => receivedByLine.TryGetValue(x.Id, out var received) && received > 0m);

            order.Status = fullyReceived ? PurchaseOrderStatus.FullyReceived : partiallyReceived ? PurchaseOrderStatus.PartiallyReceived : PurchaseOrderStatus.Confirmed;
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return Results.Created($"/api/v1/purchase-receipts/{receipt.Id}", receipt);
        });

        app.MapGet("/api/v1/purchase-receipts", async (ErpDbContext db, Guid? purchaseOrderId, CancellationToken ct) =>
        {
            var query = db.PurchaseReceipts.AsNoTracking().AsQueryable();
            if (purchaseOrderId.HasValue) query = query.Where(x => x.PurchaseOrderId == purchaseOrderId.Value);
            var receipts = await query.OrderByDescending(x => x.CreatedUtc).Take(500).ToListAsync(ct);
            return Results.Ok(new { receipts });
        });
    }
}
