namespace MAEMS.Domain.Entities;

public class Article
{
    public int ArticleId { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Thumbnail { get; set; }
    public int? AuthorId { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
