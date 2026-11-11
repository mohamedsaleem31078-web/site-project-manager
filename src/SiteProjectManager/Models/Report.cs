namespace SiteProjectManager.Models;

public class Attachment
{
    public int Id { get; set; }
    public int SnagId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Snag? Snag { get; set; }
}
