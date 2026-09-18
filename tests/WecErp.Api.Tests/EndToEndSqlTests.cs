using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using WecErp.Infrastructure;
using Xunit;

namespace WecErp.Api.Tests;

public sealed class WecErpApiFactory : WebApplicationFactory<Program>
{
    private const string SetupKey = "WEC-ERP-TEST-SETUP-2026";
    private const string JwtKey = "WEC-ERP-TEST-JWT-KEY-CHANGE-ME-32-BYTES-MINIMUM!";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connectionString = Environment.GetEnvironmentVariable("WECERP_TEST_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("WECERP_TEST_CONNECTION must be configured for SQL-backed API tests.");

        if (!connectionString.Contains("WecErp_ApiTest", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("WECERP_TEST_CONNECTION must point to the dedicated WecErp_ApiTest database.");

        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:ErpDatabase", connectionString);
        builder.UseSetting("Identity:BootstrapKey", SetupKey);
        builder.UseSetting("Identity:JwtKey", JwtKey);
        builder.UseSetting("Identity:Issuer", "WEC-ERP-Test");
        builder.UseSetting("Identity:Audience", "WEC-ERP-Test-Client");
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();

        for (var attempt = 1; attempt <= 10; attempt++)
        {
            try
            {
                await db.Database.EnsureDeletedAsync();
                await db.Database.MigrateAsync();
                return;
            }
            catch when (attempt < 10)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        await db.Database.MigrateAsync();
    }
}

public sealed class EndToEndSqlTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task SqlSchema_And_CoreErpWorkflow_WorkEndToEnd()
    {
        using var factory = new WecErpApiFactory();
        await factory.ResetDatabaseAsync();

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var live = await client.GetAsync("/api/v1/health/live");
        Assert.Equal(200, (int)live.StatusCode);

        var bootstrap = await PostJsonAsync(client, "/api/v1/auth/bootstrap", new
        {
            UserName = "admin",
            DisplayName = "SQL Test Administrator",
            Password = "WEC-ERP-Test-Password-2026!",
            Role = 1
        }, null, "WEC-ERP-TEST-SETUP-2026");
        Assert.Equal(201, (int)bootstrap.StatusCode);

        var login = await PostJsonAsync(client, "/api/v1/auth/login", new
        {
            UserName = "admin",
            Password = "WEC-ERP-Test-Password-2026!"
        });
        Assert.Equal(200, (int)login.StatusCode);
        using var loginJson = await JsonDocument.ParseAsync(await login.Content.ReadAsStreamAsync());
        var token = loginJson.RootElement.GetProperty("accessToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(token));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var customer = await PostJsonAsync(auth, "/api/v1/customers", new
        {
            Code = "SQLTEST-C001",
            Name = "SQL Integration Customer",
            Phone = "0500000000",
            Email = "sqltest@example.com",
            TaxNumber = "300000000000003"
        });
        Assert.Equal(201, (int)customer.StatusCode);
        var customerId = await GetGuidAsync(customer, "id");

        var item = await PostJsonAsync(auth, "/api/v1/items", new
        {
            Sku = "SQLTEST-P001",
            Name = "SQL Integration Product",
            Type = 1
        });
        Assert.Equal(201, (int)item.StatusCode);
        var itemId = await GetGuidAsync(item, "id");

        var warehouse = await PostJsonAsync(auth, "/api/v1/warehouses", new
        {
            Code = "SQLTEST-W001",
            Name = "SQL Integration Warehouse"
        });
        Assert.Equal(201, (int)warehouse.StatusCode);
        var warehouseId = await GetGuidAsync(warehouse, "id");

        var supplier = await PostJsonAsync(auth, "/api/v1/suppliers", new
        {
            Code = "SQLTEST-S001",
            Name = "SQL Integration Supplier",
            Phone = "0500000001",
            Email = "supplier@example.com",
            TaxNumber = "300000000000004"
        });
        Assert.Equal(201, (int)supplier.StatusCode);
        var supplierId = await GetGuidAsync(supplier, "id");

        var purchaseOrder = await PostJsonAsync(auth, "/api/v1/purchase-orders", new
        {
            SupplierId = supplierId,
            CurrencyCode = "SAR",
            Notes = "SQL integration purchase",
            Lines = new[]
            {
                new { ItemId = itemId, Description = "SQL Integration Product", Quantity = 5m, UnitCost = 100m, TaxPercent = 15m }
            }
        });
        Assert.Equal(201, (int)purchaseOrder.StatusCode);
        using var purchaseOrderJson = await ParseAsync(purchaseOrder);
        var purchaseOrderId = purchaseOrderJson.RootElement.GetProperty("id").GetGuid();
        var purchaseOrderLineId = purchaseOrderJson.RootElement.GetProperty("lines")[0].GetProperty("id").GetGuid();

        var confirmedPurchase = await PostJsonAsync(auth, $"/api/v1/purchase-orders/{purchaseOrderId}/status", new { Status = "Confirmed" });
        Assert.Equal(200, (int)confirmedPurchase.StatusCode);

        var receipt = await PostJsonAsync(auth, $"/api/v1/purchase-orders/{purchaseOrderId}/receive", new
        {
            WarehouseId = warehouseId,
            Lines = new[] { new { PurchaseOrderLineId = purchaseOrderLineId, Quantity = 5m } }
        });
        Assert.Equal(201, (int)receipt.StatusCode);

        var balance = await client.GetAsync($"/api/v1/inventory/balances?itemId={itemId}&warehouseId={warehouseId}");
        Assert.Equal(200, (int)balance.StatusCode);
        using var balanceJson = JsonDocument.Parse(await balance.Content.ReadAsStringAsync());
        var onHandAfterReceipt = balanceJson.RootElement.GetProperty("balances")[0].GetProperty("onHand").GetDecimal();
        Assert.Equal(5m, onHandAfterReceipt);

        var quotation = await PostJsonAsync(auth, "/api/v1/quotations", new
        {
            CustomerId = customerId,
            CurrencyCode = "SAR",
            Notes = "SQL integration quotation",
            Lines = new[]
            {
                new { ItemId = itemId, Description = "SQL Integration Product", Quantity = 2m, UnitPrice = 150m, DiscountPercent = 0m, TaxPercent = 15m }
            }
        });
        Assert.Equal(201, (int)quotation.StatusCode);
        using var quotationJson = await ParseAsync(quotation);
        var quotationId = quotationJson.RootElement.GetProperty("id").GetGuid();
        Assert.Equal(345m, quotationJson.RootElement.GetProperty("total").GetDecimal());

        var acceptedQuotation = await PostJsonAsync(auth, $"/api/v1/quotations/{quotationId}/status", new { Status = "Accepted" });
        Assert.Equal(200, (int)acceptedQuotation.StatusCode);

        var salesOrder = await PostJsonAsync(auth, $"/api/v1/quotations/{quotationId}/convert-to-order", new { });
        Assert.Equal(201, (int)salesOrder.StatusCode);
        var salesOrderId = await GetGuidAsync(salesOrder, "id");

        var confirmedOrder = await PostJsonAsync(auth, $"/api/v1/sales-orders/{salesOrderId}/status", new { Status = "Confirmed" });
        Assert.Equal(200, (int)confirmedOrder.StatusCode);

        var fulfilled = await PostJsonAsync(auth, $"/api/v1/sales-orders/{salesOrderId}/fulfill", new { WarehouseId = warehouseId });
        Assert.Equal(200, (int)fulfilled.StatusCode);

        var invoice = await PostJsonAsync(auth, "/api/v1/invoices/from-sales-order", new { SalesOrderId = salesOrderId });
        Assert.Equal(201, (int)invoice.StatusCode);
        var invoiceId = await GetGuidAsync(invoice, "id");

        var payment = await PostJsonAsync(auth, $"/api/v1/invoices/{invoiceId}/payments", new
        {
            Amount = 345m,
            Method = "Test",
            Reference = "SQL-TEST-PAYMENT"
        });
        Assert.Equal(201, (int)payment.StatusCode);

        var invoiceDetails = await auth.GetAsync($"/api/v1/invoices/{invoiceId}");
        Assert.Equal(200, (int)invoiceDetails.StatusCode);
        using var invoiceJson = JsonDocument.Parse(await invoiceDetails.Content.ReadAsStringAsync());
        Assert.Equal("Paid", invoiceJson.RootElement.GetProperty("status").GetString());
        Assert.Equal(345m, invoiceJson.RootElement.GetProperty("paidAmount").GetDecimal());

        var resource = await PostJsonAsync(auth, "/api/v1/resources", new
        {
            Code = "SQLTEST-R001",
            Name = "SQL Test Resource",
            Type = 1,
            IsActive = true
        });
        Assert.Equal(201, (int)resource.StatusCode);
        var resourceId = await GetGuidAsync(resource, "id");

        var start = DateTimeOffset.UtcNow.AddHours(1);
        var end = start.AddHours(1);
        var booking = await PostJsonAsync(auth, "/api/v1/bookings", new
        {
            CustomerId = customerId,
            ItemId = itemId,
            ResourceId = resourceId,
            StartsUtc = start,
            EndsUtc = end,
            Notes = "SQL test booking"
        });
        Assert.Equal(201, (int)booking.StatusCode);

        var overlappingBooking = await PostJsonAsync(auth, "/api/v1/bookings", new
        {
            CustomerId = customerId,
            ItemId = itemId,
            ResourceId = resourceId,
            StartsUtc = start.AddMinutes(30),
            EndsUtc = end.AddMinutes(30),
            Notes = "Should conflict"
        });
        Assert.Equal(409, (int)overlappingBooking.StatusCode);

        var project = await PostJsonAsync(auth, "/api/v1/projects", new
        {
            CustomerId = customerId,
            Name = "SQL Integration Project",
            StartsOn = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            EndsOn = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(30)),
            BudgetAmount = 10000m,
            CurrencyCode = "SAR",
            Notes = "SQL integration project"
        });
        Assert.Equal(201, (int)project.StatusCode);
        var projectId = await GetGuidAsync(project, "id");

        var task = await PostJsonAsync(auth, $"/api/v1/projects/{projectId}/tasks", new
        {
            Name = "SQL integration task",
            Sequence = 1,
            EstimatedCost = 500m
        });
        Assert.Equal(201, (int)task.StatusCode);
        var taskId = await GetGuidAsync(task, "id");

        var completedTask = await PostJsonAsync(auth, $"/api/v1/projects/{projectId}/tasks/{taskId}/completion", new { Completed = true });
        Assert.Equal(200, (int)completedTask.StatusCode);

        var activatedProject = await PostJsonAsync(auth, $"/api/v1/projects/{projectId}/status", new { Status = "Active" });
        Assert.Equal(200, (int)activatedProject.StatusCode);

        var serviceContract = await PostJsonAsync(auth, "/api/v1/service-contracts", new
        {
            CustomerId = customerId,
            StartsOn = DateOnly.FromDateTime(DateTime.UtcNow.Date),
            EndsOn = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(30)),
            VisitFrequencyPerWeek = 1,
            Notes = "SQL integration service contract"
        });
        Assert.Equal(201, (int)serviceContract.StatusCode);
        var serviceContractId = await GetGuidAsync(serviceContract, "id");

        var activatedContract = await PostJsonAsync(auth, $"/api/v1/service-contracts/{serviceContractId}/activate", new { });
        Assert.Equal(200, (int)activatedContract.StatusCode);

        var generated = await PostJsonAsync(auth, $"/api/v1/service-contracts/{serviceContractId}/generate-work-orders", new
        {
            FirstVisitUtc = DateTimeOffset.UtcNow.AddHours(2),
            DurationMinutes = 60,
            Description = "SQL integration maintenance visit"
        });
        Assert.Equal(201, (int)generated.StatusCode);
        using var generatedJson = await ParseAsync(generated);
        Assert.True(generatedJson.RootElement.GetProperty("generated").GetInt32() > 0);

        var finalBalance = await auth.GetAsync($"/api/v1/inventory/balances?itemId={itemId}&warehouseId={warehouseId}");
        Assert.Equal(200, (int)finalBalance.StatusCode);
        using var finalBalanceJson = JsonDocument.Parse(await finalBalance.Content.ReadAsStringAsync());
        var onHandAfterSale = finalBalanceJson.RootElement.GetProperty("balances")[0].GetProperty("onHand").GetDecimal();
        Assert.Equal(3m, onHandAfterSale);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        Assert.True(await db.Database.CanConnectAsync());
        Assert.True(await db.AuditLogs.AnyAsync(x => x.UserName == "admin" && x.Path == "/api/v1/customers"));

        var migrations = await db.Database.GetAppliedMigrationsAsync();
        Assert.Contains("20260918141526_InitialCreate", migrations);

        var expectedTables = new[]
        {
            "Items", "Customers", "Users", "AuditLogs", "ServiceContracts", "WorkOrders",
            "CustomerSites", "ServiceAssets", "Suppliers", "Quotations", "QuotationLines",
            "SalesOrders", "SalesOrderLines", "Resources", "Bookings", "Warehouses",
            "InventoryMovements", "PurchaseOrders", "PurchaseOrderLines", "PurchaseReceipts",
            "PurchaseReceiptLines", "Invoices", "InvoiceLines", "Payments", "Projects", "ProjectTasks"
        };

        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();
        foreach (var table in expectedTables)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @tableName";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@tableName";
            parameter.Value = table;
            command.Parameters.Add(parameter);
            var exists = Convert.ToInt32(await command.ExecuteScalarAsync());
            Assert.Equal(1, exists);
        }
    }

    private static async Task<HttpResponseMessage> PostJsonAsync(HttpClient client, string path, object body, HttpMethod? method = null, string? setupKey = null)
    {
        using var request = new HttpRequestMessage(method ?? HttpMethod.Post, path)
        {
            Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrWhiteSpace(setupKey))
            request.Headers.Add("X-WEC-Setup-Key", setupKey);

        return await client.SendAsync(request);
    }

    private static async Task<Guid> GetGuidAsync(HttpResponseMessage response, string propertyName)
    {
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        return json.RootElement.GetProperty(propertyName).GetGuid();
    }

    private static async Task<JsonDocument> ParseAsync(HttpResponseMessage response)
        => await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
}
