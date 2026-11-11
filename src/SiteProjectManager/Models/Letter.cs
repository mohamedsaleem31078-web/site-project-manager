namespace SiteProjectManager.Models;

public sealed class Letter
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int? TemplateId { get; set; }
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Project? Project { get; set; }
}
