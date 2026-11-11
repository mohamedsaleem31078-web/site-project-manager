using Microsoft.EntityFrameworkCore;
using SiteProjectManager.Data;
using SiteProjectManager.Models;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=site-project-manager.db";

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy" }));

app.MapGet("/api/projects", async (AppDbContext db, CancellationToken ct) =>
    await db.Projects.AsNoTracking()
        .OrderByDescending(p => p.UpdatedAt)
        .Select(p => new ProjectView(
            p.Id,
            p.Name,
            p.ClientName,
            p.SiteName,
            p.Status,
            db.Snags.Count(s => s.ProjectId == p.Id),
            db.Snags.Count(s => s.ProjectId == p.Id && s.Status != "Closed")))
        .ToListAsync(ct));

app.MapPost("/api/projects", async Task<IResult> (ProjectInput input, AppDbContext db, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(input.Name))
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["اسم المشروع مطلوب."] });

    var project = new Project
    {
        Name = input.Name.Trim(),
        ClientName = input.ClientName?.Trim() ?? string.Empty,
        SiteName = input.SiteName?.Trim() ?? string.Empty,
        Address = input.Address?.Trim() ?? string.Empty,
        StartDate = input.StartDate ?? DateTime.UtcNow.Date,
        EndDate = input.EndDate,
        Notes = input.Notes?.Trim() ?? string.Empty,
        Status = "In Progress",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    db.Projects.Add(project);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/api/projects/{project.Id}", new ProjectView(project.Id, project.Name, project.ClientName, project.SiteName, project.Status, 0, 0));
});

app.MapGet("/api/snags", async (int? projectId, AppDbContext db, CancellationToken ct) =>
{
    var query = db.Snags.AsNoTracking();
    if (projectId.HasValue)
        query = query.Where(s => s.ProjectId == projectId.Value);

    return await query.OrderByDescending(s => s.CreatedAt)
        .Select(s => new SnagView(s.Id, s.ProjectId, s.Title, s.Description, s.Category, s.Priority, s.Status, s.LocationDescription, s.AssignedTo, s.CreatedAt, s.DueDate))
        .ToListAsync(ct);
});

app.MapPost("/api/snags", async Task<IResult> (SnagInput input, AppDbContext db, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(input.Title))
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["title"] = ["عنوان الملاحظة مطلوب."] });

    if (!await db.Projects.AnyAsync(p => p.Id == input.ProjectId, ct))
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["projectId"] = ["اختر مشروعًا موجودًا."] });

    var snag = new Snag
    {
        ProjectId = input.ProjectId,
        Title = input.Title.Trim(),
        Description = input.Description?.Trim() ?? string.Empty,
        Category = input.Category?.Trim() ?? "عام",
        Priority = input.Priority is "Low" or "Medium" or "High" or "Critical" ? input.Priority : "Medium",
        Status = "Open",
        LocationDescription = input.LocationDescription?.Trim() ?? string.Empty,
        AssignedTo = input.AssignedTo?.Trim() ?? string.Empty,
        DueDate = input.DueDate,
        CreatedAt = DateTime.UtcNow
    };

    db.Snags.Add(snag);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/api/snags/{snag.Id}", new SnagView(snag.Id, snag.ProjectId, snag.Title, snag.Description, snag.Category, snag.Priority, snag.Status, snag.LocationDescription, snag.AssignedTo, snag.CreatedAt, snag.DueDate));
});

app.MapPut("/api/snags/{id:int}/status", async Task<IResult> (int id, StatusInput input, AppDbContext db, CancellationToken ct) =>
{
    if (input.Status is not ("Open" or "In Progress" or "Closed"))
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["الحالة غير صالحة."] });

    var snag = await db.Snags.FindAsync([id], ct);
    if (snag is null) return Results.NotFound();

    snag.Status = input.Status;
    snag.IsClosed = input.Status == "Closed";
    snag.ClosedAt = snag.IsClosed ? DateTime.UtcNow : null;
    await db.SaveChangesAsync(ct);
    return Results.NoContent();
});

app.MapGet("/api/reports", async (int? projectId, AppDbContext db, CancellationToken ct) =>
{
    var query = db.Reports.AsNoTracking();
    if (projectId.HasValue)
        query = query.Where(r => r.ProjectId == projectId.Value);

    return await query.OrderByDescending(r => r.GeneratedAt)
        .Select(r => new ReportView(r.Id, r.ProjectId, r.Type, r.Title, r.Content, r.GeneratedAt))
        .ToListAsync(ct);
});

app.MapPost("/api/reports", async Task<IResult> (ReportInput input, AppDbContext db, CancellationToken ct) =>
{
    if (!await db.Projects.AnyAsync(p => p.Id == input.ProjectId, ct))
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["projectId"] = ["اختر مشروعًا موجودًا."] });

    var report = new Report
    {
        ProjectId = input.ProjectId,
        Type = input.Type?.Trim() ?? "Summary",
        Title = input.Title?.Trim() ?? "تقرير موجز",
        Content = input.Content?.Trim() ?? string.Empty,
        GeneratedAt = DateTime.UtcNow
    };

    db.Reports.Add(report);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/api/reports/{report.Id}", new ReportView(report.Id, report.ProjectId, report.Type, report.Title, report.Content, report.GeneratedAt));
});

app.MapGet("/api/letters", async (int? projectId, AppDbContext db, CancellationToken ct) =>
{
    var query = db.Letters.AsNoTracking();
    if (projectId.HasValue)
        query = query.Where(l => l.ProjectId == projectId.Value);

    return await query.OrderByDescending(l => l.CreatedAt)
        .Select(l => new LetterView(l.Id, l.ProjectId, l.Subject, l.Body, l.CreatedAt))
        .ToListAsync(ct);
});

app.MapPost("/api/letters", async Task<IResult> (LetterInput input, AppDbContext db, CancellationToken ct) =>
{
    if (!await db.Projects.AnyAsync(p => p.Id == input.ProjectId, ct))
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["projectId"] = ["اختر مشروعًا موجودًا."] });

    var letter = new Letter
    {
        ProjectId = input.ProjectId,
        Subject = input.Subject?.Trim() ?? "خطاب رسمي",
        Body = input.Body?.Trim() ?? string.Empty,
        CreatedAt = DateTime.UtcNow
    };

    db.Letters.Add(letter);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/api/letters/{letter.Id}", new LetterView(letter.Id, letter.ProjectId, letter.Subject, letter.Body, letter.CreatedAt));
});

app.MapGet("/api/templates", async (AppDbContext db, CancellationToken ct) =>
    await db.LetterTemplates.AsNoTracking().OrderBy(t => t.Name).ToListAsync(ct));

app.MapFallbackToFile("index.html");
app.Run();

internal sealed record ProjectInput(string Name, string? ClientName, string? SiteName, string? Address, DateTime? StartDate, DateTime? EndDate, string? Notes);
internal sealed record ProjectView(int Id, string Name, string ClientName, string SiteName, string Status, int SnagCount, int OpenSnagCount);
internal sealed record SnagInput(int ProjectId, string Title, string? Description, string? Category, string? Priority, string? LocationDescription, string? AssignedTo, DateTime? DueDate);
internal sealed record StatusInput(string Status);
internal sealed record SnagView(int Id, int ProjectId, string Title, string Description, string Category, string Priority, string Status, string LocationDescription, string AssignedTo, DateTime CreatedAt, DateTime? DueDate);
internal sealed record ReportInput(int ProjectId, string? Type, string? Title, string? Content);
internal sealed record ReportView(int Id, int ProjectId, string Type, string Title, string Content, DateTime GeneratedAt);
internal sealed record LetterInput(int ProjectId, string? Subject, string? Body);
internal sealed record LetterView(int Id, int ProjectId, string Subject, string Body, DateTime CreatedAt);
