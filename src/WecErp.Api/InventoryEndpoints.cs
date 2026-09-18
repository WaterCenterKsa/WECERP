using Microsoft.EntityFrameworkCore;
using WecErp.Application.Inventory;
using WecErp.Domain;
using WecErp.Infrastructure;

public static class InventoryEndpoints
{
    public static void MapInventoryEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/warehouses", async (
            ErpDbContext db,
            int? page,
            int? pageSize,
            string? search,
            CancellationToken cancellationToken) =>
        {
            var currentPage = Math.Max(page ?? 1, 1);
            var size = Math.Clamp(pageSize ?? 50, 1, 200);
            var query = db.Warehouses.AsNoTracking().Where(x => x.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x => x.Code.Contains(term) || x.Name.Contains(term));
            }

            var total = await query.CountAsync(cancellationToken);
            var warehouses = await query
                .OrderBy(x => x.Name)
                .ThenBy(x => x.Code)
                .Skip((currentPage - 1) * size)
                .Take(size)
                .ToListAsync(cancellationToken);

            return Results.Ok(new { page = currentPage, pageSize = size, total, warehouses });
        });

        app.MapPost("/api/v1/warehouses", async (
            CreateWarehouseRequest request,
            InventoryService service,
            ErpDbContext db,
            CancellationToken cancellationToken) =>
        {
            var validationError = service.ValidateWarehouse(request);
            if (!string.IsNullOrEmpty(validationError))
                return Results.BadRequest(new { error = validationError });

            var code = request.Code.Trim();
            if (await db.Warehouses.AnyAsync(x => x.Code == code, cancellationToken))
                return Results.Conflict(new { error = "A warehouse with this code already exists." });

            var warehouse = new Warehouse
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = request.Name.Trim(),
                IsActive = true,
                CreatedUtc = DateTimeOffset.UtcNow
            };

            db.Warehouses.Add(warehouse);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Created($"/api/v1/warehouses/{warehouse.Id}", warehouse);
        });

        app.MapGet("/api/v1/inventory/balances", async (
            ErpDbContext db,
            Guid? itemId,
            Guid? warehouseId,
            CancellationToken cancellationToken) =>
        {
            var movements = db.InventoryMovements.AsNoTracking();

            if (itemId.HasValue)
                movements = movements.Where(x => x.ItemId == itemId.Value);
            if (warehouseId.HasValue)
                movements = movements.Where(x => x.WarehouseId == warehouseId.Value);

            var rows = await movements
                .GroupBy(x => new { x.ItemId, x.WarehouseId })
                .Select(g => new InventoryBalanceDto
                {
                    ItemId = g.Key.ItemId,
                    WarehouseId = g.Key.WarehouseId,
                    OnHand =
                        g.Where(x => x.Type == InventoryMovementType.Receipt ||
                                     x.Type == InventoryMovementType.TransferIn ||
                                     x.Type == InventoryMovementType.AdjustmentIncrease)
                         .Sum(x => x.Quantity)
                        -
                        g.Where(x => x.Type == InventoryMovementType.Issue ||
                                     x.Type == InventoryMovementType.TransferOut ||
                                     x.Type == InventoryMovementType.AdjustmentDecrease)
                         .Sum(x => x.Quantity),
                    Reserved =
                        g.Where(x => x.Type == InventoryMovementType.Reservation)
                         .Sum(x => x.Quantity)
                        -
                        g.Where(x => x.Type == InventoryMovementType.ReleaseReservation)
                         .Sum(x => x.Quantity)
                })
                .ToListAsync(cancellationToken);

            foreach (var row in rows)
                row.Available = row.OnHand - row.Reserved;

            return Results.Ok(new { balances = rows });
        });

        app.MapGet("/api/v1/inventory/movements", async (
            ErpDbContext db,
            Guid? itemId,
            Guid? warehouseId,
            int? page,
            int? pageSize,
            CancellationToken cancellationToken) =>
        {
            var currentPage = Math.Max(page ?? 1, 1);
            var size = Math.Clamp(pageSize ?? 100, 1, 500);
            var query = db.InventoryMovements.AsNoTracking();

            if (itemId.HasValue)
                query = query.Where(x => x.ItemId == itemId.Value);
            if (warehouseId.HasValue)
                query = query.Where(x => x.WarehouseId == warehouseId.Value);

            var total = await query.CountAsync(cancellationToken);
            var movements = await query
                .OrderByDescending(x => x.CreatedUtc)
                .Skip((currentPage - 1) * size)
                .Take(size)
                .ToListAsync(cancellationToken);

            return Results.Ok(new { page = currentPage, pageSize = size, total, movements });
        });

        app.MapPost("/api/v1/inventory/movements", async (
            CreateInventoryMovementRequest request,
            InventoryService service,
            ErpDbContext db,
            CancellationToken cancellationToken) =>
        {
            var validationError = service.ValidateMovement(request);
            if (!string.IsNullOrEmpty(validationError))
                return Results.BadRequest(new { error = validationError });

            var item = await db.Items.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.ItemId && x.IsActive, cancellationToken);
            if (item is null)
                return Results.BadRequest(new { error = "Item does not exist or is inactive." });

            var warehouse = await db.Warehouses.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == request.WarehouseId && x.IsActive, cancellationToken);
            if (warehouse is null)
                return Results.BadRequest(new { error = "Warehouse does not exist or is inactive." });

            if (request.TransferWarehouseId.HasValue)
            {
                var destination = await db.Warehouses.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == request.TransferWarehouseId.Value && x.IsActive, cancellationToken);
                if (destination is null)
                    return Results.BadRequest(new { error = "Transfer warehouse does not exist or is inactive." });
            }

            var signedOnHand = await CalculateOnHand(db, request.ItemId, request.WarehouseId, cancellationToken);
            var reserved = await CalculateReserved(db, request.ItemId, request.WarehouseId, cancellationToken);

            if (request.Type == InventoryMovementType.Issue ||
                request.Type == InventoryMovementType.TransferOut ||
                request.Type == InventoryMovementType.AdjustmentDecrease)
            {
                if (signedOnHand - reserved < request.Quantity)
                    return Results.Conflict(new { error = "Insufficient available stock." });
            }

            if (request.Type == InventoryMovementType.ReleaseReservation && reserved < request.Quantity)
                return Results.Conflict(new { error = "Cannot release more stock than is currently reserved." });

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            var movement = service.CreateMovement(request, request.Type, request.WarehouseId);
            db.InventoryMovements.Add(movement);

            InventoryMovement? transferIn = null;
            if (request.Type == InventoryMovementType.TransferOut)
            {
                transferIn = service.CreateMovement(request, InventoryMovementType.TransferIn, request.TransferWarehouseId!.Value);
                db.InventoryMovements.Add(transferIn);
            }
            else if (request.Type == InventoryMovementType.TransferIn)
            {
                return Results.BadRequest(new { error = "Use TransferOut to create a complete warehouse transfer." });
            }

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Results.Created($"/api/v1/inventory/movements/{movement.Id}", new
            {
                movement,
                transferIn
            });
        });
    }

    private static async Task<decimal> CalculateOnHand(
        ErpDbContext db,
        Guid itemId,
        Guid warehouseId,
        CancellationToken cancellationToken)
    {
        return await db.InventoryMovements
            .Where(x => x.ItemId == itemId && x.WarehouseId == warehouseId)
            .SumAsync(x =>
                x.Type == InventoryMovementType.Receipt ||
                x.Type == InventoryMovementType.TransferIn ||
                x.Type == InventoryMovementType.AdjustmentIncrease
                    ? x.Quantity
                    : x.Type == InventoryMovementType.Issue ||
                      x.Type == InventoryMovementType.TransferOut ||
                      x.Type == InventoryMovementType.AdjustmentDecrease
                        ? -x.Quantity
                        : 0D,
                cancellationToken);
    }

    private static async Task<decimal> CalculateReserved(
        ErpDbContext db,
        Guid itemId,
        Guid warehouseId,
        CancellationToken cancellationToken)
    {
        return await db.InventoryMovements
            .Where(x => x.ItemId == itemId && x.WarehouseId == warehouseId)
            .SumAsync(x =>
                x.Type == InventoryMovementType.Reservation
                    ? x.Quantity
                    : x.Type == InventoryMovementType.ReleaseReservation
                        ? -x.Quantity
                        : 0D,
                cancellationToken);
    }
}
