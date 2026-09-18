using Microsoft.EntityFrameworkCore;
using WecErp.Application.Service;
using WecErp.Domain;
using WecErp.Infrastructure;

public static class ServiceEndpoints
{
    public static void MapServiceEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/service-contracts", async (ErpDbContext db, int? page, int? pageSize, Guid? customerId, CancellationToken ct) =>
        {
            var currentPage = Math.Max(page ?? 1, 1);
            var size = Math.Clamp(pageSize ?? 50, 1, 200);
            var query = db.ServiceContracts.AsNoTracking();
            if (customerId.HasValue) query = query.Where(x => x.CustomerId == customerId.Value);
            var total = await query.CountAsync(ct);
            var contracts = await query.OrderByDescending(x => x.CreatedUtc).Skip((currentPage - 1) * size).Take(size).ToListAsync(ct);
            return Results.Ok(new { page = currentPage, pageSize = size, total, serviceContracts = contracts });
        });

        app.MapPost("/api/v1/service-contracts", async (CreateServiceContractRequest request, ServiceService service, ErpDbContext db, CancellationToken ct) =>
        {
            var error = service.ValidateContract(request);
            if (!string.IsNullOrEmpty(error)) return Results.BadRequest(new { error });
            if (!await db.Customers.AnyAsync(x => x.Id == request.CustomerId && x.IsActive, ct))
                return Results.BadRequest(new { error = "Customer does not exist or is inactive." });
            var contract = service.CreateContract(request);
            db.ServiceContracts.Add(contract);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/service-contracts/{contract.Id}", contract);
        });

        app.MapPost("/api/v1/service-contracts/{id:guid}/activate", async (Guid id, ErpDbContext db, CancellationToken ct) =>
        {
            var contract = await db.ServiceContracts.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (contract is null) return Results.NotFound(new { error = "Service contract not found." });
            if (contract.Status != ServiceContractStatus.Draft) return Results.BadRequest(new { error = "Only draft contracts can be activated." });
            if (contract.StartsOn > DateOnly.FromDateTime(DateTime.UtcNow)) return Results.BadRequest(new { error = "A contract cannot be activated before its start date." });
            contract.Status = ServiceContractStatus.Active;
            await db.SaveChangesAsync(ct);
            return Results.Ok(contract);
        });

        app.MapGet("/api/v1/work-orders", async (ErpDbContext db, int? page, int? pageSize, Guid? customerId, Guid? serviceContractId, CancellationToken ct) =>
        {
            var currentPage = Math.Max(page ?? 1, 1);
            var size = Math.Clamp(pageSize ?? 50, 1, 200);
            var query = db.WorkOrders.AsNoTracking();
            if (customerId.HasValue) query = query.Where(x => x.CustomerId == customerId.Value);
            if (serviceContractId.HasValue) query = query.Where(x => x.ServiceContractId == serviceContractId.Value);
            var total = await query.CountAsync(ct);
            var orders = await query.OrderByDescending(x => x.CreatedUtc).Skip((currentPage - 1) * size).Take(size).ToListAsync(ct);
            return Results.Ok(new { page = currentPage, pageSize = size, total, workOrders = orders });
        });

        app.MapPost("/api/v1/work-orders", async (CreateWorkOrderRequest request, ServiceService service, ErpDbContext db, CancellationToken ct) =>
        {
            if (!await db.Customers.AnyAsync(x => x.Id == request.CustomerId && x.IsActive, ct))
                return Results.BadRequest(new { error = "Customer does not exist or is inactive." });
            if (request.ServiceContractId.HasValue && !await db.ServiceContracts.AnyAsync(x => x.Id == request.ServiceContractId.Value && x.Status == ServiceContractStatus.Active, ct))
                return Results.BadRequest(new { error = "Service contract does not exist or is not active." });

            WorkOrder order;
            try { order = service.CreateWorkOrder(request); }
            catch (InvalidOperationException ex) { return Results.BadRequest(new { error = ex.Message }); }

            db.WorkOrders.Add(order);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/work-orders/{order.Id}", order);
        });

        app.MapPost("/api/v1/work-orders/{id:guid}/status", async (Guid id, ChangeWorkOrderStatusRequest request, ServiceService service, ErpDbContext db, CancellationToken ct) =>
        {
            var order = await db.WorkOrders.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (order is null) return Results.NotFound(new { error = "Work order not found." });
            var error = String.Empty;
            if (!service.TryChangeWorkOrderStatus(order, request.Status, ref error))
                return Results.BadRequest(new { error });
            await db.SaveChangesAsync(ct);
            return Results.Ok(order);
        });
    }
}
