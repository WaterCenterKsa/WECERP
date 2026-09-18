using Microsoft.EntityFrameworkCore;
using WecErp.Application.Projects;
using WecErp.Domain;
using WecErp.Infrastructure;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/projects", async (ErpDbContext db, int? page, int? pageSize, Guid? customerId, CancellationToken ct) =>
        {
            var currentPage = Math.Max(page ?? 1, 1);
            var size = Math.Clamp(pageSize ?? 50, 1, 200);
            var query = db.Projects.AsNoTracking();
            if (customerId.HasValue) query = query.Where(x => x.CustomerId == customerId.Value);
            var total = await query.CountAsync(ct);
            var projects = await query.Include(x => x.Tasks).OrderByDescending(x => x.CreatedUtc).Skip((currentPage - 1) * size).Take(size).ToListAsync(ct);
            return Results.Ok(new { page = currentPage, pageSize = size, total, projects });
        });

        app.MapPost("/api/v1/projects", async (CreateProjectRequest request, ProjectService service, ErpDbContext db, CancellationToken ct) =>
        {
            var error = service.ValidateProject(request);
            if (!string.IsNullOrEmpty(error)) return Results.BadRequest(new { error });
            if (!await db.Customers.AnyAsync(x => x.Id == request.CustomerId && x.IsActive, ct))
                return Results.BadRequest(new { error = "Customer does not exist or is inactive." });
            var project = service.CreateProject(request);
            db.Projects.Add(project);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/projects/{project.Id}", project);
        });

        app.MapPost("/api/v1/projects/{id:guid}/status", async (Guid id, ChangeProjectStatusRequest request, ProjectService service, ErpDbContext db, CancellationToken ct) =>
        {
            var project = await db.Projects.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (project is null) return Results.NotFound(new { error = "Project not found." });
            var error = String.Empty;
            if (!service.TryChangeStatus(project, request.Status, ref error)) return Results.BadRequest(new { error });
            await db.SaveChangesAsync(ct);
            return Results.Ok(project);
        });

        app.MapPost("/api/v1/projects/{id:guid}/tasks", async (Guid id, CreateProjectTaskRequest request, ProjectService service, ErpDbContext db, CancellationToken ct) =>
        {
            var project = await db.Projects.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (project is null) return Results.NotFound(new { error = "Project not found." });
            var error = service.ValidateTask(request);
            if (!string.IsNullOrEmpty(error)) return Results.BadRequest(new { error });
            var task = service.CreateTask(project.Id, request);
            db.ProjectTasks.Add(task);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/projects/{id}/tasks/{task.Id}", task);
        });

        app.MapPost("/api/v1/projects/{id:guid}/tasks/{taskId:guid}/completion", async (Guid id, Guid taskId, CompleteProjectTaskRequest request, ErpDbContext db, CancellationToken ct) =>
        {
            var task = await db.ProjectTasks.FirstOrDefaultAsync(x => x.Id == taskId && x.ProjectId == id, ct);
            if (task is null) return Results.NotFound(new { error = "Project task not found." });
            task.IsCompleted = request.Completed;
            await db.SaveChangesAsync(ct);
            return Results.Ok(task);
        });
    }
}
