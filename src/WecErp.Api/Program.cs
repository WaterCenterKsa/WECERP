using Microsoft.EntityFrameworkCore;
using WecErp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("ErpDatabase")
    ?? throw new InvalidOperationException("ConnectionStrings:ErpDatabase is not configured.");

builder.Services.AddDbContext<ErpDbContext>(options =>
    options.UseSqlServer(connectionString));

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

app.Run();

public partial class Program { }
