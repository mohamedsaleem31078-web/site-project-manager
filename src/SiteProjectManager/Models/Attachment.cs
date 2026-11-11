namespace SiteProjectManager.Models;

public class Snag
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium";
    public string Status { get; set; } = "Open";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public string AssignedTo { get; set; } = string.Empty;
    public string LocationDescription { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsClosed { get; set; }
    public DateTime? ClosedAt { get; set; }

    public Project? Project { get; set; }
    public List<Attachment> Attachments { get; set; } = new();
}

