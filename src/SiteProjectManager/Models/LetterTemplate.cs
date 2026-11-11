namespace SiteProjectManager.Models;

public class Letter
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int TemplateId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Project? Project { get; set; }
}
