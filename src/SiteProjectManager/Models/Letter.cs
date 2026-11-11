namespace SiteProjectManager.Models;

public class Report
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Type { get; set; } = "Summary";
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public Project? Project { get; set; }
}
