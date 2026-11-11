namespace SiteProjectManager.Models;

public sealed class Snag
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "عام";
    public string Priority { get; set; } = "Medium";
    public string Status { get; set; } = "Open";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public string AssignedTo { get; set; } = "";
    public string LocationDescription { get; set; } = "";
    public string Notes { get; set; } = "";
    public bool IsClosed { get; set; }
    public DateTime? ClosedAt { get; set; }
    public Project? Project { get; set; }
    public List<Attachment> Attachments { get; set; } = [];
}
