namespace SiteProjectManager.Models;

public sealed class Attachment
{
    public int Id { get; set; }
    public int SnagId { get; set; }
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string FileType { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public Snag? Snag { get; set; }
}
