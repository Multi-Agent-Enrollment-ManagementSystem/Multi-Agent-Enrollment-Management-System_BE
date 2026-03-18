namespace MAEMS.MultiAgent.RAG.Models;

/// <summary>
/// Document model for RAG system
/// </summary>
public class RagDocument
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Content { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // "database" or "file"
    public Dictionary<string, string> Metadata { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}
