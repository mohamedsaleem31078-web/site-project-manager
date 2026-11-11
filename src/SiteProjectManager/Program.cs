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
    await db.Projects.AsNoTracking().OrderByDescending(p => p.UpdatedAt)
        .Select(p => new ProjectView(p.Id, p.Name, p.ClientName, p.SiteName, p.Status,
            db.Snags.Count(s => s.ProjectId == p.Id),
            db.Snags.Count(s => s.ProjectId == p.Id && s.Status != "Closed")))
        .ToListAsync(ct));

app.MapPost("/api/projects", async Task<IResult> (ProjectInput input, AppDbContext db, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(input.Name))
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["name"] = ["اسم المشروع مطلوب."] });
    var project = new Project
    {
        Name = input.Name.Trim(), ClientName = input.ClientName?.Trim() ?? "",
        SiteName = input.SiteName?.Trim() ?? "", Address = input.Address?.Trim() ?? "",
        StartDate = input.StartDate ?? DateTime.UtcNow.Date, EndDate = input.EndDate,
        Notes = input.Notes?.Trim() ?? "", Status = "In Progress",
        CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
    };
    db.Projects.Add(project);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/api/projects/{project.Id}", new ProjectView(project.Id, project.Name,
        project.ClientName, project.SiteName, project.Status, 0, 0));
});

app.MapGet("/api/snags", async (int? projectId, AppDbContext db, CancellationToken ct) =>
{
    var query = db.Snags.AsNoTracking();
    if (projectId.HasValue) query = query.Where(s => s.ProjectId == projectId.Value);
    return await query.OrderByDescending(s => s.CreatedAt)
        .Select(s => new SnagView(s.Id, s.ProjectId, s.Title, s.Description, s.Category,
            s.Priority, s.Status, s.LocationDescription, s.AssignedTo, s.CreatedAt, s.DueDate,
            db.Attachments.Where(a => a.SnagId == s.Id)
                .Select(a => new AttachmentView(a.Id, a.FileName, a.FileType, a.Description)).ToList()))
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
        ProjectId = input.ProjectId, Title = input.Title.Trim(),
        Description = input.Description?.Trim() ?? "", Category = input.Category?.Trim() ?? "عام",
        Priority = input.Priority is "Low" or "Medium" or "High" or "Critical" ? input.Priority : "Medium",
        Status = "Open", LocationDescription = input.LocationDescription?.Trim() ?? "",
        AssignedTo = input.AssignedTo?.Trim() ?? "", DueDate = input.DueDate, CreatedAt = DateTime.UtcNow
    };
    db.Snags.Add(snag);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/api/snags/{snag.Id}", new SnagView(snag.Id, snag.ProjectId, snag.Title,
        snag.Description, snag.Category, snag.Priority, snag.Status, snag.LocationDescription,
        snag.AssignedTo, snag.CreatedAt, snag.DueDate, []));
});

app.MapPost("/api/snags/{id:int}/attachments", async Task<IResult> (int id, HttpRequest request,
    AppDbContext db, IWebHostEnvironment environment, CancellationToken ct) =>
{
    var snag = await db.Snags.FindAsync([id], ct);
    if (snag is null) return Results.NotFound(new { message = "الملاحظة غير موجودة." });
    if (!request.HasFormContentType) return Results.BadRequest(new { message = "يجب إرسال ملف بصيغة multipart/form-data." });

    var form = await request.ReadFormAsync(ct);
    var file = form.Files.GetFile("file");
    if (file is null || file.Length == 0) return Results.BadRequest(new { message = "اختر صورة أو ملف PDF." });
    if (file.Length > 10 * 1024 * 1024) return Results.BadRequest(new { message = "الحد الأقصى لحجم الملف 10 ميجابايت." });

    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
    var allowed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".png"] = "image/png",
        [".webp"] = "image/webp", [".pdf"] = "application/pdf"
    };
    if (!allowed.TryGetValue(extension, out var expectedType) || !string.Equals(file.ContentType, expectedType, StringComparison.OrdinalIgnoreCase))
        return Results.BadRequest(new { message = "الملفات المسموح بها: JPG أو PNG أو WEBP أو PDF." });

    var storageDirectory = Path.Combine(environment.ContentRootPath, "App_Data", "uploads");
    Directory.CreateDirectory(storageDirectory);
    var storedName = $"{Guid.NewGuid():N}{extension}";
    var fullPath = Path.Combine(storageDirectory, storedName);
    try
    {
        await using var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await file.CopyToAsync(stream, ct);
        stream.Position = 0;
        if (!await HasExpectedSignatureAsync(stream, extension, ct))
        {
            stream.Close();
            File.Delete(fullPath);
            return Results.BadRequest(new { message = "محتوى الملف لا يطابق امتداده." });
        }
    }
    catch
    {
        if (File.Exists(fullPath)) File.Delete(fullPath);
        throw;
    }

    var attachment = new Attachment
    {
        SnagId = id,
        FileName = Path.GetFileName(file.FileName),
        FilePath = storedName,
        FileType = expectedType,
        Description = Path.GetFileNameWithoutExtension(file.FileName),
        UploadedAt = DateTime.UtcNow
    };
    db.Attachments.Add(attachment);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/api/attachments/{attachment.Id}",
        new AttachmentView(attachment.Id, attachment.FileName, attachment.FileType, attachment.Description));
});

app.MapGet("/api/attachments/{id:int}", async Task<IResult> (int id, AppDbContext db,
    IWebHostEnvironment environment, CancellationToken ct) =>
{
    var attachment = await db.Attachments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
    if (attachment is null) return Results.NotFound();
    var extension = Path.GetExtension(attachment.FilePath);
    if (extension.Length != 5 && extension.Length != 4) return Results.NotFound();
    var fullPath = Path.Combine(environment.ContentRootPath, "App_Data", "uploads", Path.GetFileName(attachment.FilePath));
    if (!File.Exists(fullPath)) return Results.NotFound();
    var contentType = attachment.FileType is "image/jpeg" or "image/png" or "image/webp" or "application/pdf"
        ? attachment.FileType : "application/octet-stream";
    return Results.File(fullPath, contentType, enableRangeProcessing: true);
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
    if (projectId.HasValue) query = query.Where(r => r.ProjectId == projectId.Value);
    return await query.OrderByDescending(r => r.GeneratedAt)
        .Select(r => new ReportView(r.Id, r.ProjectId, r.Type, r.Title, r.Content, r.GeneratedAt))
        .ToListAsync(ct);
});
app.MapPost("/api/reports", async Task<IResult> (ReportInput input, AppDbContext db, CancellationToken ct) =>
{
    if (!await db.Projects.AnyAsync(p => p.Id == input.ProjectId, ct))
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["projectId"] = ["اختر مشروعًا موجودًا."] });
    var report = new Report { ProjectId = input.ProjectId, Type = input.Type?.Trim() ?? "Summary",
        Title = input.Title?.Trim() ?? "تقرير موجز", Content = input.Content?.Trim() ?? "", GeneratedAt = DateTime.UtcNow };
    db.Reports.Add(report);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/api/reports/{report.Id}", new ReportView(report.Id, report.ProjectId, report.Type, report.Title, report.Content, report.GeneratedAt));
});
app.MapGet("/api/letters", async (int? projectId, AppDbContext db, CancellationToken ct) =>
{
    var query = db.Letters.AsNoTracking();
    if (projectId.HasValue) query = query.Where(l => l.ProjectId == projectId.Value);
    return await query.OrderByDescending(l => l.CreatedAt)
        .Select(l => new LetterView(l.Id, l.ProjectId, l.Subject, l.Body, l.CreatedAt))
        .ToListAsync(ct);
});
app.MapPost("/api/letters", async Task<IResult> (LetterInput input, AppDbContext db, CancellationToken ct) =>
{
    if (!await db.Projects.AnyAsync(p => p.Id == input.ProjectId, ct))
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["projectId"] = ["اختر مشروعًا موجودًا."] });
    var letter = new Letter { ProjectId = input.ProjectId, Subject = input.Subject?.Trim() ?? "خطاب رسمي",
        Body = input.Body?.Trim() ?? "", CreatedAt = DateTime.UtcNow };
    db.Letters.Add(letter);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/api/letters/{letter.Id}", new LetterView(letter.Id, letter.ProjectId, letter.Subject, letter.Body, letter.CreatedAt));
});
app.MapGet("/api/templates", async (AppDbContext db, CancellationToken ct) =>
    await db.LetterTemplates.AsNoTracking().OrderBy(t => t.Name).ToListAsync(ct));
app.MapFallbackToFile("index.html");
app.Run();

static async Task<bool> HasExpectedSignatureAsync(Stream stream, string extension, CancellationToken ct)
{
    var header = new byte[12];
    var read = await stream.ReadAsync(header.AsMemory(0, header.Length), ct);
    return extension switch
    {
        ".jpg" or ".jpeg" => read >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
        ".png" => read >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
        ".webp" => read >= 12 && header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8),
        ".pdf" => read >= 5 && header.AsSpan(0, 5).SequenceEqual("%PDF-"u8),
        _ => false
    };
}

internal sealed record ProjectInput(string Name, string? ClientName, string? SiteName, string? Address, DateTime? StartDate, DateTime? EndDate, string? Notes);
internal sealed record ProjectView(int Id, string Name, string ClientName, string SiteName, string Status, int SnagCount, int OpenSnagCount);
internal sealed record SnagInput(int ProjectId, string Title, string? Description, string? Category, string? Priority, string? LocationDescription, string? AssignedTo, DateTime? DueDate);
internal sealed record StatusInput(string Status);
internal sealed record SnagView(int Id, int ProjectId, string Title, string Description, string Category, string Priority, string Status, string LocationDescription, string AssignedTo, DateTime CreatedAt, DateTime? DueDate, List<AttachmentView> Attachments);
internal sealed record AttachmentView(int Id, string FileName, string FileType, string Description);
internal sealed record ReportInput(int ProjectId, string? Type, string? Title, string? Content);
internal sealed record ReportView(int Id, int ProjectId, string Type, string Title, string Content, DateTime GeneratedAt);
internal sealed record LetterInput(int ProjectId, string? Subject, string? Body);
internal sealed record LetterView(int Id, int ProjectId, string Subject, string Body, DateTime CreatedAt);
