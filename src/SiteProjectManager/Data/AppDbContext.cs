using SiteProjectManager.Data;
using SiteProjectManager.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=siteprojectmanager.db"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.MapGet("/health", () => Results.Ok(new { status = "Healthy" }));

app.MapGet("/api/projects", async (AppDbContext db) =>
    await db.Projects
        .Include(p => p.Snags)
        .OrderBy(p => p.Name)
        .ToListAsync());

app.MapGet("/api/projects/{id:int}", async (int id, AppDbContext db) =>
    await db.Projects
        .Include(p => p.Snags)
        .FirstOrDefaultAsync(p => p.Id == id) is Project project
        ? Results.Ok(project)
        : Results.NotFound());

app.MapPost("/api/projects", async (Project project, AppDbContext db) =>
{
    project.CreatedAt = DateTime.UtcNow;
    project.UpdatedAt = DateTime.UtcNow;
    db.Projects.Add(project);
    await db.SaveChangesAsync();
    return Results.Created($"/api/projects/{project.Id}", project);
});

app.MapPut("/api/projects/{id:int}", async (int id, Project updatedProject, AppDbContext db) =>
{
    var project = await db.Projects.FindAsync(id);
    if (project is null)
    {
        return Results.NotFound();
    }

    project.Name = updatedProject.Name;
    project.ClientName = updatedProject.ClientName;
    project.SiteName = updatedProject.SiteName;
    project.Address = updatedProject.Address;
    project.StartDate = updatedProject.StartDate;
    project.EndDate = updatedProject.EndDate;
    project.Status = updatedProject.Status;
    project.Notes = updatedProject.Notes;
    project.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();
    return Results.Ok(project);
});

app.MapDelete("/api/projects/{id:int}", async (int id, AppDbContext db) =>
{
    var project = await db.Projects.FindAsync(id);
    if (project is null)
    {
        return Results.NotFound();
    }

    db.Projects.Remove(project);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapGet("/api/snags", async (AppDbContext db) =>
    await db.Snags
        .Include(s => s.Attachments)
        .OrderByDescending(s => s.CreatedAt)
        .ToListAsync());

app.MapGet("/api/snags/project/{projectId:int}", async (int projectId, AppDbContext db) =>
    await db.Snags
        .Where(s => s.ProjectId == projectId)
        .Include(s => s.Attachments)
        .OrderByDescending(s => s.CreatedAt)
        .ToListAsync());

app.MapPost("/api/snags", async (Snag snag, AppDbContext db) =>
{
    snag.CreatedAt = DateTime.UtcNow;
    db.Snags.Add(snag);
    await db.SaveChangesAsync();
    return Results.Created($"/api/snags/{snag.Id}", snag);
});

app.MapPut("/api/snags/{id:int}", async (int id, Snag updatedSnag, AppDbContext db) =>
{
    var snag = await db.Snags.FindAsync(id);
    if (snag is null) return Results.NotFound();

    snag.ProjectId = updatedSnag.ProjectId;
    snag.Title = updatedSnag.Title;
    snag.Description = updatedSnag.Description;
    snag.Category = updatedSnag.Category;
    snag.Priority = updatedSnag.Priority;
    snag.Status = updatedSnag.Status;
    snag.DueDate = updatedSnag.DueDate;
    snag.AssignedTo = updatedSnag.AssignedTo;
    snag.LocationDescription = updatedSnag.LocationDescription;
    snag.Notes = updatedSnag.Notes;
    snag.IsClosed = updatedSnag.IsClosed;
    snag.ClosedAt = updatedSnag.ClosedAt;

    await db.SaveChangesAsync();
    return Results.Ok(snag);
});

app.MapPost("/api/snags/{id:int}/attachments", async (int id, Attachment attachment, AppDbContext db) =>
{
    var snag = await db.Snags.FindAsync(id);
    if (snag is null)
    {
        return Results.NotFound();
    }

    attachment.SnagId = id;
    attachment.UploadedAt = DateTime.UtcNow;
    db.Attachments.Add(attachment);
    await db.SaveChangesAsync();
    return Results.Created($"/api/snags/{id}/attachments/{attachment.Id}", attachment);
});

app.MapGet("/api/reports", async (AppDbContext db) =>
    await db.Reports.OrderByDescending(r => r.GeneratedAt).ToListAsync());

app.MapPost("/api/reports", async (Report report, AppDbContext db) =>
{
    report.GeneratedAt = DateTime.UtcNow;
    db.Reports.Add(report);
    await db.SaveChangesAsync();
    return Results.Created($"/api/reports/{report.Id}", report);
});

app.MapGet("/api/letters", async (AppDbContext db) =>
    await db.Letters.OrderByDescending(l => l.CreatedAt).ToListAsync());

app.MapPost("/api/letters", async (Letter letter, AppDbContext db) =>
{
    letter.CreatedAt = DateTime.UtcNow;
    db.Letters.Add(letter);
    await db.SaveChangesAsync();
    return Results.Created($"/api/letters/{letter.Id}", letter);
});

app.MapGet("/api/templates", async (AppDbContext db) =>
    await db.LetterTemplates.OrderBy(t => t.Name).ToListAsync());

app.MapPost("/api/templates", async (LetterTemplate template, AppDbContext db) =>
{
    db.LetterTemplates.Add(template);
    await db.SaveChangesAsync();
    return Results.Created($"/api/templates/{template.Id}", template);
});

app.Run();
