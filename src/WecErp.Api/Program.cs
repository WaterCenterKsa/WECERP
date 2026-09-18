using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using WecErp.Application.Bookings;
using WecErp.Application.Customers;
using WecErp.Application.Items;
using WecErp.Application.Inventory;
using WecErp.Application.Invoices;
using WecErp.Application.Quotations;
using WecErp.Application.Suppliers;
using WecErp.Application.Purchasing;
using WecErp.Application.SalesOrders;
using WecErp.Application.Service;
using WecErp.Application.Projects;
using WecErp.Application.Identity;
using WecErp.Domain;
using WecErp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("ErpDatabase")
    ?? throw new InvalidOperationException("ConnectionStrings:ErpDatabase is not configured.");

if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("ConnectionStrings:ErpDatabase cannot be empty.");

builder.Services.AddDbContext<ErpDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly("WecErp.Infrastructure.Migrations")));
builder.Services.AddSingleton<CustomerService>();
builder.Services.AddSingleton<ItemService>();
builder.Services.AddSingleton<QuotationService>();
builder.Services.AddSingleton<SalesOrderService>();
builder.Services.AddSingleton<BookingService>();
builder.Services.AddSingleton<InventoryService>();
builder.Services.AddSingleton<SupplierService>();
builder.Services.AddSingleton<PurchaseOrderService>();
builder.Services.AddSingleton<InvoiceService>();
builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddSingleton<UserService>();
builder.Services.AddSingleton<ServiceService>();
builder.Services.AddSingleton<ProjectService>();
builder.Services.AddProblemDetails();

var jwtKey = builder.Configuration["Identity:JwtKey"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    if (builder.Environment.IsDevelopment())
        jwtKey = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    else
        throw new InvalidOperationException("Identity:JwtKey must be configured with at least 32 UTF-8 bytes.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Identity:Issuer"] ?? "WEC-ERP",
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Identity:Audience"] ?? "WEC-ERP-Desktop",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
    await db.Database.MigrateAsync();
}

if (!app.Environment.IsEnvironment("Testing"))
    app.UseHttpsRedirection();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    finally
    {
        if (context.Request.Path.StartsWithSegments("/api/v1") &&
            !context.Request.Path.StartsWithSegments("/api/v1/health") &&
            !context.Request.Path.StartsWithSegments("/api/v1/auth"))
        {
            try
            {
                await using var auditScope = app.Services.CreateAsyncScope();
                var auditDb = auditScope.ServiceProvider.GetRequiredService<ErpDbContext>();

                Guid? userId = null;
                var userIdValue = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(userIdValue, out var parsedUserId))
                    userId = parsedUserId;

                var userName = context.User.Identity?.Name
                    ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                    ?? "anonymous";

                auditDb.AuditLogs.Add(new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    UserName = userName.Length > 100 ? userName[..100] : userName,
                    Action = context.Request.Method,
                    Path = context.Request.Path.Value?.Length > 500
                        ? context.Request.Path.Value[..500]
                        : context.Request.Path.Value ?? string.Empty,
                    Method = context.Request.Method.Length > 10
                        ? context.Request.Method[..10]
                        : context.Request.Method,
                    StatusCode = context.Response.StatusCode,
                    CreatedUtc = DateTimeOffset.UtcNow
                });

                await auditDb.SaveChangesAsync();
            }
            catch
            {
                // Auditing must never turn a successful business request into a failed request.
            }
        }
    }
});

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/v1") &&
        !context.Request.Path.StartsWithSegments("/api/v1/health") &&
        !context.Request.Path.StartsWithSegments("/api/v1/auth"))
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var path = context.Request.Path.Value ?? String.Empty;
        var role = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var allowed = path.StartsWith("/api/v1/inventory/", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/api/v1/warehouses", StringComparison.OrdinalIgnoreCase)
            ? role is "Administrator" or "Manager" or "Warehouse"
            : path.StartsWith("/api/v1/purchase-orders", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/api/v1/suppliers", StringComparison.OrdinalIgnoreCase)
                ? role is "Administrator" or "Manager" or "Purchasing"
                : path.StartsWith("/api/v1/invoices", StringComparison.OrdinalIgnoreCase)
                    ? role is "Administrator" or "Manager" or "Accountant"
                    : path.StartsWith("/api/v1/service-contracts", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/api/v1/work-orders", StringComparison.OrdinalIgnoreCase)
                        ? role is "Administrator" or "Manager" or "Service"
                        : path.StartsWith("/api/v1/audit-logs", StringComparison.OrdinalIgnoreCase)
                            ? role is "Administrator" or "Manager"
                            : true;

        if (!allowed)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }
    }
    await next();
});

app.MapGet("/api/v1/health/live", () => Results.Ok(new
{
    status = "ok",
    service = "WEC ERP API",
    utc = DateTimeOffset.UtcNow
}));

app.MapGet("/api/v1/health/ready", async (ErpDbContext db, CancellationToken cancellationToken) =>
{
    var databaseReady = await db.Database.CanConnectAsync(cancellationToken);

    return databaseReady
        ? Results.Ok(new { status = "ready", database = "connected" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
});

app.MapIdentityEndpoints();
app.MapInventoryEndpoints();
app.MapPurchasingEndpoints();
app.MapSalesOrderEndpoints();
app.MapInvoiceEndpoints();
app.MapServiceEndpoints();
app.MapProjectEndpoints();

app.MapGet("/api/v1/audit-logs", async (ErpDbContext db, int? page, int? pageSize, CancellationToken ct) =>
{
    var currentPage = Math.Max(page ?? 1, 1);
    var size = Math.Clamp(pageSize ?? 50, 1, 200);
    var logs = await db.AuditLogs.AsNoTracking().OrderByDescending(x => x.CreatedUtc)
        .Skip((currentPage - 1) * size).Take(size).ToListAsync(ct);
    return Results.Ok(new { page = currentPage, pageSize = size, logs });
}).RequireAuthorization();

app.MapGet("/api/v1/items", async (
    ErpDbContext db,
    int? page,
    int? pageSize,
    string? search,
    CancellationToken cancellationToken) =>
{
    var currentPage = Math.Max(page ?? 1, 1);
    var size = Math.Clamp(pageSize ?? 50, 1, 200);
    var query = db.Items.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(search))
    {
        var term = search.Trim();
        query = query.Where(x => x.Sku.Contains(term) || x.Name.Contains(term));
    }

    var total = await query.CountAsync(cancellationToken);
    var items = await query
        .OrderBy(x => x.Name)
        .ThenBy(x => x.Sku)
        .Skip((currentPage - 1) * size)
        .Take(size)
        .Select(x => new ItemDto
        {
            Id = x.Id,
            Sku = x.Sku,
            Name = x.Name,
            Type = x.Type,
            IsActive = x.IsActive
        })
        .ToListAsync(cancellationToken);

    return Results.Ok(new { page = currentPage, pageSize = size, total, items });
});

app.MapPost("/api/v1/items", async (
    CreateItemRequest request,
    [FromServices] ItemService service,
    ErpDbContext db,
    CancellationToken cancellationToken) =>
{
    var validationError = service.ValidateNewItem(request);
    if (!string.IsNullOrEmpty(validationError))
        return Results.BadRequest(new { error = validationError });

    var sku = request.Sku.Trim();
    var exists = await db.Items.AnyAsync(x => x.Sku == sku, cancellationToken);
    if (exists)
        return Results.Conflict(new { error = "An item with this SKU already exists." });

    var item = service.CreateEntity(request);
    db.Items.Add(item);
    await db.SaveChangesAsync(cancellationToken);

    return Results.Created($"/api/v1/items/{item.Id}", service.ToDto(item));
});

app.MapGet("/api/v1/customers", async (
    ErpDbContext db,
    int? page,
    int? pageSize,
    string? search,
    CancellationToken cancellationToken) =>
{
    var currentPage = Math.Max(page ?? 1, 1);
    var size = Math.Clamp(pageSize ?? 50, 1, 200);
    var query = db.Customers.AsNoTracking().Where(x => x.IsActive);

    if (!string.IsNullOrWhiteSpace(search))
    {
        var term = search.Trim();
        query = query.Where(x => x.Code.Contains(term) || x.Name.Contains(term) || x.Phone.Contains(term));
    }

    var total = await query.CountAsync(cancellationToken);
    var customers = await query
        .OrderBy(x => x.Name)
        .ThenBy(x => x.Code)
        .Skip((currentPage - 1) * size)
        .Take(size)
        .Select(x => new CustomerDto
        {
            Id = x.Id,
            Code = x.Code,
            Name = x.Name,
            Phone = x.Phone,
            Email = x.Email,
            TaxNumber = x.TaxNumber,
            IsActive = x.IsActive,
            CreatedUtc = x.CreatedUtc
        })
        .ToListAsync(cancellationToken);

    return Results.Ok(new { page = currentPage, pageSize = size, total, customers });
});

app.MapPost("/api/v1/customers", async (
    CreateCustomerRequest request,
    [FromServices] CustomerService service,
    ErpDbContext db,
    CancellationToken cancellationToken) =>
{
    var validationError = service.ValidateNewCustomer(request);
    if (!string.IsNullOrEmpty(validationError))
        return Results.BadRequest(new { error = validationError });

    var code = request.Code.Trim();
    var exists = await db.Customers.AnyAsync(x => x.Code == code, cancellationToken);
    if (exists)
        return Results.Conflict(new { error = "A customer with this code already exists." });

    var customer = service.CreateEntity(request);
    db.Customers.Add(customer);
    await db.SaveChangesAsync(cancellationToken);

    return Results.Created($"/api/v1/customers/{customer.Id}", service.ToDto(customer));
});

app.MapGet("/api/v1/quotations", async (
    ErpDbContext db,
    int? page,
    int? pageSize,
    string? search,
    CancellationToken cancellationToken) =>
{
    var currentPage = Math.Max(page ?? 1, 1);
    var size = Math.Clamp(pageSize ?? 50, 1, 200);
    var query = db.Quotations.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(search))
    {
        var term = search.Trim();
        query = query.Where(x => x.Number.Contains(term));
    }

    var total = await query.CountAsync(cancellationToken);
    var quotations = await query
        .OrderByDescending(x => x.CreatedUtc)
        .Skip((currentPage - 1) * size)
        .Take(size)
        .Select(x => new
        {
            x.Id,
            x.Number,
            x.CustomerId,
            x.Status,
            x.CurrencyCode,
            x.Subtotal,
            x.DiscountAmount,
            x.TaxAmount,
            x.Total,
            x.ValidUntil,
            x.CreatedUtc
        })
        .ToListAsync(cancellationToken);

    return Results.Ok(new { page = currentPage, pageSize = size, total, quotations });
});

app.MapGet("/api/v1/quotations/{id:guid}", async (
    Guid id,
    ErpDbContext db,
    CancellationToken cancellationToken) =>
{
    var quotation = await db.Quotations
        .AsNoTracking()
        .Include(x => x.Lines)
        .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    return quotation is null
        ? Results.NotFound(new { error = "Quotation not found." })
        : Results.Ok(quotation);
});

app.MapPost("/api/v1/quotations", async (
    CreateQuotationRequest request,
    [FromServices] QuotationService service,
    ErpDbContext db,
    CancellationToken cancellationToken) =>
{
    var validationError = service.ValidateNewQuotation(request);
    if (!string.IsNullOrEmpty(validationError))
        return Results.BadRequest(new { error = validationError });

    var customerExists = await db.Customers.AnyAsync(x => x.Id == request.CustomerId && x.IsActive, cancellationToken);
    if (!customerExists)
        return Results.BadRequest(new { error = "Customer does not exist or is inactive." });

    var itemIds = request.Lines.Select(x => x.ItemId).Distinct().ToList();
    var items = await db.Items
        .Where(x => itemIds.Contains(x.Id) && x.IsActive)
        .ToDictionaryAsync(x => x.Id, cancellationToken);

    if (items.Count != itemIds.Count)
        return Results.BadRequest(new { error = "One or more quotation items do not exist or are inactive." });

    var quotation = service.CreateEntity(request, items);
    db.Quotations.Add(quotation);
    await db.SaveChangesAsync(cancellationToken);

    return Results.Created($"/api/v1/quotations/{quotation.Id}", service.ToDto(quotation));
});

app.MapPost("/api/v1/quotations/{id:guid}/status", async (
    Guid id,
    ChangeQuotationStatusRequest request,
    [FromServices] QuotationService service,
    ErpDbContext db,
    CancellationToken cancellationToken) =>
{
    var quotation = await db.Quotations
        .Include(x => x.Lines)
        .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    if (quotation is null)
        return Results.NotFound(new { error = "Quotation not found." });

    var errorMessage = String.Empty;
    var changed = service.TryChangeStatus(quotation, request.Status, ref errorMessage);
    if (!changed)
        return Results.BadRequest(new { error = errorMessage });

    await db.SaveChangesAsync(cancellationToken);
    return Results.Ok(service.ToDto(quotation));
});

app.MapGet("/api/v1/sales-orders", async (
    ErpDbContext db,
    int? page,
    int? pageSize,
    string? search,
    CancellationToken cancellationToken) =>
{
    var currentPage = Math.Max(page ?? 1, 1);
    var size = Math.Clamp(pageSize ?? 50, 1, 200);
    var query = db.SalesOrders.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(search))
    {
        var term = search.Trim();
        query = query.Where(x => x.Number.Contains(term));
    }

    var total = await query.CountAsync(cancellationToken);
    var orders = await query
        .OrderByDescending(x => x.CreatedUtc)
        .Skip((currentPage - 1) * size)
        .Take(size)
        .Select(x => new
        {
            x.Id,
            x.Number,
            x.CustomerId,
            x.SourceQuotationId,
            x.Status,
            x.CurrencyCode,
            x.Subtotal,
            x.DiscountAmount,
            x.TaxAmount,
            x.Total,
            x.CreatedUtc
        })
        .ToListAsync(cancellationToken);

    return Results.Ok(new { page = currentPage, pageSize = size, total, salesOrders = orders });
});

app.MapGet("/api/v1/sales-orders/{id:guid}", async (
    Guid id,
    ErpDbContext db,
    CancellationToken cancellationToken) =>
{
    var order = await db.SalesOrders
        .AsNoTracking()
        .Include(x => x.Lines)
        .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    return order is null
        ? Results.NotFound(new { error = "Sales order not found." })
        : Results.Ok(order);
});

app.MapPost("/api/v1/quotations/{id:guid}/convert-to-order", async (
    Guid id,
    [FromServices] SalesOrderService service,
    ErpDbContext db,
    CancellationToken cancellationToken) =>
{
    await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

    var quotation = await db.Quotations
        .Include(x => x.Lines)
        .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    if (quotation is null)
        return Results.NotFound(new { error = "Quotation not found." });

    var existingOrder = await db.SalesOrders
        .AsNoTracking()
        .FirstOrDefaultAsync(x => x.SourceQuotationId == quotation.Id, cancellationToken);

    if (existingOrder is not null)
        return Results.Conflict(new { error = "This quotation has already been converted to a sales order.", salesOrderId = existingOrder.Id });

    if (quotation.Status != QuotationStatus.Accepted)
        return Results.BadRequest(new { error = "Only accepted quotations can be converted to sales orders." });

    var order = service.CreateFromAcceptedQuotation(quotation);
    db.SalesOrders.Add(order);
    await db.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);

    return Results.Created($"/api/v1/sales-orders/{order.Id}", service.ToDto(order));
});

app.MapGet("/api/v1/resources", async (
    ErpDbContext db,
    int? page,
    int? pageSize,
    string? search,
    CancellationToken cancellationToken) =>
{
    var currentPage = Math.Max(page ?? 1, 1);
    var size = Math.Clamp(pageSize ?? 50, 1, 200);
    var query = db.Resources.AsNoTracking().Where(x => x.IsActive);

    if (!string.IsNullOrWhiteSpace(search))
    {
        var term = search.Trim();
        query = query.Where(x => x.Code.Contains(term) || x.Name.Contains(term));
    }

    var total = await query.CountAsync(cancellationToken);
    var resources = await query
        .OrderBy(x => x.Name)
        .ThenBy(x => x.Code)
        .Skip((currentPage - 1) * size)
        .Take(size)
        .ToListAsync(cancellationToken);

    return Results.Ok(new { page = currentPage, pageSize = size, total, resources });
});

app.MapPost("/api/v1/resources", async (
    Resource request,
    ErpDbContext db,
    CancellationToken cancellationToken) =>
{
    if (String.IsNullOrWhiteSpace(request.Code) || String.IsNullOrWhiteSpace(request.Name))
        return Results.BadRequest(new { error = "Code and Name are required." });

    var code = request.Code.Trim();
    if (await db.Resources.AnyAsync(x => x.Code == code, cancellationToken))
        return Results.Conflict(new { error = "A resource with this code already exists." });

    var resource = new Resource
    {
        Id = Guid.NewGuid(),
        Code = code,
        Name = request.Name.Trim(),
        Type = request.Type,
        IsActive = true
    };

    db.Resources.Add(resource);
    await db.SaveChangesAsync(cancellationToken);
    return Results.Created($"/api/v1/resources/{resource.Id}", resource);
});

app.MapGet("/api/v1/bookings", async (
    ErpDbContext db,
    DateTimeOffset? fromUtc,
    DateTimeOffset? toUtc,
    Guid? resourceId,
    CancellationToken cancellationToken) =>
{
    var query = db.Bookings.AsNoTracking();

    if (fromUtc.HasValue)
        query = query.Where(x => x.EndsUtc > fromUtc.Value);
    if (toUtc.HasValue)
        query = query.Where(x => x.StartsUtc < toUtc.Value);
    if (resourceId.HasValue)
        query = query.Where(x => x.ResourceId == resourceId.Value);

    var bookings = await query
        .OrderBy(x => x.StartsUtc)
        .Take(500)
        .ToListAsync(cancellationToken);

    return Results.Ok(new { bookings });
});

app.MapPost("/api/v1/bookings", async (
    CreateBookingRequest request,
    [FromServices] BookingService service,
    ErpDbContext db,
    CancellationToken cancellationToken) =>
{
    var validationError = service.ValidateNewBooking(request);
    if (!String.IsNullOrEmpty(validationError))
        return Results.BadRequest(new { error = validationError });

    var customerExists = await db.Customers.AnyAsync(x => x.Id == request.CustomerId && x.IsActive, cancellationToken);
    if (!customerExists)
        return Results.BadRequest(new { error = "Customer does not exist or is inactive." });

    var itemExists = await db.Items.AnyAsync(x => x.Id == request.ItemId && x.IsActive, cancellationToken);
    if (!itemExists)
        return Results.BadRequest(new { error = "Item does not exist or is inactive." });

    if (request.ResourceId.HasValue)
    {
        var resourceExists = await db.Resources.AnyAsync(x => x.Id == request.ResourceId.Value && x.IsActive, cancellationToken);
        if (!resourceExists)
            return Results.BadRequest(new { error = "Resource does not exist or is inactive." });
    }

    if (request.ResourceId.HasValue)
    {
        var conflict = await db.Bookings.AnyAsync(x =>
            x.ResourceId == request.ResourceId.Value &&
            x.Status != BookingStatus.Cancelled &&
            x.StartsUtc < request.EndsUtc &&
            x.EndsUtc > request.StartsUtc,
            cancellationToken);

        if (conflict)
            return Results.Conflict(new { error = "The selected resource is already booked during this time." });
    }

    var booking = service.CreateEntity(request);
    db.Bookings.Add(booking);
    await db.SaveChangesAsync(cancellationToken);

    return Results.Created($"/api/v1/bookings/{booking.Id}", service.ToDto(booking));
});

app.Run();

public partial class Program { }
