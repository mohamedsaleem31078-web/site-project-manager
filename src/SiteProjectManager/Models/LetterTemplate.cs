namespace SiteProjectManager.Models;

public sealed class LetterTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public bool IsDefault { get; set; }
}
