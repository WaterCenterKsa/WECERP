using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WecErp.Application.Identity;
using WecErp.Domain;
using WecErp.Infrastructure;

public static class IdentityEndpoints
{
    public static void MapIdentityEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/auth/setup-required", async (ErpDbContext db, CancellationToken ct) =>
            Results.Ok(new { setupRequired = !await db.Users.AnyAsync(ct) }));

        app.MapPost("/api/v1/auth/bootstrap", async (
            CreateUserRequest request,
            IConfiguration configuration,
            HttpContext httpContext,
            UserService service,
            ErpDbContext db,
            CancellationToken ct) =>
        {
            if (await db.Users.AnyAsync(ct))
                return Results.Conflict(new { error = "ERP is already initialized." });

            var setupKey = configuration["Identity:BootstrapKey"];
            if (string.IsNullOrWhiteSpace(setupKey))
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);

            if (!string.Equals(request.Password, request.Password.Trim(), StringComparison.Ordinal))
                return Results.BadRequest(new { error = "Password contains leading or trailing whitespace." });

            var suppliedKey = httpContext.Request.Headers["X-WEC-Setup-Key"].FirstOrDefault();
            if (!string.Equals(suppliedKey, setupKey, StringComparison.Ordinal))
                return Results.Unauthorized();

            request.Role = UserRole.Administrator;
            var error = service.ValidateNewUser(request);
            if (!string.IsNullOrEmpty(error)) return Results.BadRequest(new { error });

            var user = service.CreateEntity(request);
            db.Users.Add(user);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/users/{user.Id}", service.ToDto(user));
        });

        app.MapPost("/api/v1/auth/login", async (
            LoginRequest request,
            IConfiguration configuration,
            PasswordHasher hasher,
            ErpDbContext db,
            CancellationToken ct) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(x => x.UserName == request.UserName.Trim() && x.IsActive, ct);
            if (user is null || !hasher.Verify(request.Password, user.PasswordHash))
                return Results.Unauthorized();

            var key = configuration["Identity:JwtKey"];
            if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);

            user.LastLoginUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };
            var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: configuration["Identity:Issuer"] ?? "WEC-ERP",
                audience: configuration["Identity:Audience"] ?? "WEC-ERP-Desktop",
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: credentials);

            return Results.Ok(new { accessToken = new JwtSecurityTokenHandler().WriteToken(token), expiresUtc = token.ValidTo, user = new UserDto {
                Id = user.Id, UserName = user.UserName, DisplayName = user.DisplayName, Role = user.Role,
                IsActive = user.IsActive, CreatedUtc = user.CreatedUtc, LastLoginUtc = user.LastLoginUtc
            }});
        });

        app.MapGet("/api/v1/users", async (ErpDbContext db, CancellationToken ct) =>
            Results.Ok(new { users = await db.Users.AsNoTracking().OrderBy(x => x.UserName).Select(x => new UserDto {
                Id = x.Id, UserName = x.UserName, DisplayName = x.DisplayName, Role = x.Role,
                IsActive = x.IsActive, CreatedUtc = x.CreatedUtc, LastLoginUtc = x.LastLoginUtc
            }).ToListAsync(ct) }));
    }
}
