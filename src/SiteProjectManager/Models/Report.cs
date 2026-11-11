namespace SiteProjectManager.Models;

public sealed class Report
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Type { get; set; } = "Summary";
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public Project? Project { get; set; }
}
