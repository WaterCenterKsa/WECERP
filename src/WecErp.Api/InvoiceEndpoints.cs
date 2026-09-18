using System.Data;
using Microsoft.EntityFrameworkCore;
using WecErp.Application.Invoices;
using WecErp.Domain;
using WecErp.Infrastructure;

public static class InvoiceEndpoints
{
    public static void MapInvoiceEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/invoices", async (ErpDbContext db, int? page, int? pageSize, string? search, CancellationToken ct) =>
        {
            var currentPage = Math.Max(page ?? 1, 1);
            var size = Math.Clamp(pageSize ?? 50, 1, 200);
            var query = db.Invoices.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(x => x.Number.Contains(term));
            }
            var total = await query.CountAsync(ct);
            var invoices = await query.OrderByDescending(x => x.CreatedUtc)
                .Skip((currentPage - 1) * size).Take(size)
                .Select(x => new { x.Id, x.Number, x.CustomerId, x.SourceSalesOrderId, x.Status, x.CurrencyCode, x.Total, x.PaidAmount, x.CreatedUtc })
                .ToListAsync(ct);
            return Results.Ok(new { page = currentPage, pageSize = size, total, invoices });
        });

        app.MapGet("/api/v1/invoices/{id:guid}", async (Guid id, ErpDbContext db, CancellationToken ct) =>
        {
            var invoice = await db.Invoices.AsNoTracking().Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            return invoice is null ? Results.NotFound(new { error = "Invoice not found." }) : Results.Ok(invoice);
        });

        app.MapPost("/api/v1/invoices/from-sales-order", async (
            CreateInvoiceFromSalesOrderRequest request,
            InvoiceService service,
            ErpDbContext db,
            CancellationToken ct) =>
        {
            if (request.SalesOrderId == Guid.Empty)
                return Results.BadRequest(new { error = "SalesOrderId is required." });

            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

            var order = await db.SalesOrders.Include(x => x.Lines)
                .FirstOrDefaultAsync(x => x.Id == request.SalesOrderId, ct);
            if (order is null) return Results.NotFound(new { error = "Sales order not found." });

            var existing = await db.Invoices.AsNoTracking()
                .FirstOrDefaultAsync(x => x.SourceSalesOrderId == order.Id, ct);
            if (existing is not null)
                return Results.Conflict(new { error = "This sales order has already been invoiced.", invoiceId = existing.Id });

            Invoice invoice;
            try
            {
                invoice = service.CreateFromSalesOrder(order);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }

            db.Invoices.Add(invoice);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return Results.Created($"/api/v1/invoices/{invoice.Id}", invoice);
        });

        app.MapPost("/api/v1/invoices/{id:guid}/payments", async (
            Guid id,
            RecordPaymentRequest request,
            ErpDbContext db,
            CancellationToken ct) =>
        {
            if (request.Amount <= 0m) return Results.BadRequest(new { error = "Payment amount must be greater than zero." });
            if (string.IsNullOrWhiteSpace(request.Method)) return Results.BadRequest(new { error = "Payment method is required." });

            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

            var invoice = await db.Invoices.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (invoice is null) return Results.NotFound(new { error = "Invoice not found." });
            if (invoice.Status == InvoiceStatus.Void) return Results.BadRequest(new { error = "Void invoices cannot receive payments." });

            var remaining = invoice.Total - invoice.PaidAmount;
            if (request.Amount > remaining)
                return Results.Conflict(new { error = "Payment exceeds the outstanding invoice balance.", outstanding = remaining });

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                Number = $"PAY-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant()}",
                InvoiceId = invoice.Id,
                Amount = Decimal.Round(request.Amount, 2, MidpointRounding.AwayFromZero),
                Method = request.Method.Trim(),
                Reference = request.Reference.Trim(),
                CreatedUtc = DateTimeOffset.UtcNow
            };

            invoice.PaidAmount = Decimal.Round(invoice.PaidAmount + payment.Amount, 2, MidpointRounding.AwayFromZero);
            invoice.Status = invoice.PaidAmount >= invoice.Total ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;

            db.Payments.Add(payment);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return Results.Created($"/api/v1/payments/{payment.Id}", payment);
        });

        app.MapGet("/api/v1/invoices/{id:guid}/payments", async (Guid id, ErpDbContext db, CancellationToken ct) =>
        {
            var exists = await db.Invoices.AnyAsync(x => x.Id == id, ct);
            if (!exists) return Results.NotFound(new { error = "Invoice not found." });

            var payments = await db.Payments.AsNoTracking()
                .Where(x => x.InvoiceId == id)
                .OrderBy(x => x.CreatedUtc)
                .ToListAsync(ct);
            return Results.Ok(new { payments });
        });
    }
}
