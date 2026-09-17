using Microsoft.EntityFrameworkCore;
using WecErp.Application.Customers;
using WecErp.Application.Items;
using WecErp.Application.Quotations;
using WecErp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("ErpDatabase")
    ?? throw new InvalidOperationException("ConnectionStrings:ErpDatabase is not configured.");

builder.Services.AddDbContext<ErpDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddSingleton<CustomerService>();
builder.Services.AddSingleton<ItemService>();
builder.Services.AddSingleton<QuotationService>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseExceptionHandler();

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

    return Results.Ok(new
    {
        page = currentPage,
        pageSize = size,
        total,
        items
    });
});

app.MapPost("/api/v1/items", async (
    CreateItemRequest request,
    ItemService service,
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

    return Results.Ok(new
    {
        page = currentPage,
        pageSize = size,
        total,
        customers
    });
});

app.MapPost("/api/v1/customers", async (
    CreateCustomerRequest request,
    CustomerService service,
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
    QuotationService service,
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

app.Run();

public partial class Program { }
