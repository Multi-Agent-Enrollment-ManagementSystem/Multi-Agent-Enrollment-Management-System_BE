namespace MAEMS.Application.DTOs.Chat;

/// <summary>
/// Request DTO for searching documents in RAG
/// </summary>
public class SearchRequest
{
    /// <summary>
    /// Search query text
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Number of top results to return (default: 5, max: 20)
    /// </summary>
    public int? TopK { get; set; }
}
