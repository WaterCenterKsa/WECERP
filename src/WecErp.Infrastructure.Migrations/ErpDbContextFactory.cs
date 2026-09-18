using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using WecErp.Infrastructure;

namespace WecErp.Infrastructure.Migrations;

public sealed class ErpDbContextFactory : IDesignTimeDbContextFactory<ErpDbContext>
{
    public ErpDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("WECERP_DESIGN_CONNECTION")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=WecErp_Dev;Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseSqlServer(
                connectionString,
                sql => sql.MigrationsAssembly(typeof(ErpDbContextFactory).Assembly.FullName))
            .Options;

        return new ErpDbContext(options);
    }
}
