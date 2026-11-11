namespace SiteProjectManager.Models;

public sealed class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string ClientName { get; set; } = "";
    public string SiteName { get; set; } = "";
    public string Address { get; set; } = "";
    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime? EndDate { get; set; }
    public string Status { get; set; } = "Planned";
    public string Notes { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<Snag> Snags { get; set; } = [];
}
