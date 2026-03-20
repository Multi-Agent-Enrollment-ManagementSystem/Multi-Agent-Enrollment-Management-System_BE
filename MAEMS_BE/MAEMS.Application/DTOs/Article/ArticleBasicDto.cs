namespace MAEMS.Application.DTOs.Article;

public class ArticleBasicDto
{
    public int ArticleId { get; set; }
    public string? Title { get; set; }
    public string? Thumbnail { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? Status { get; set; }
}
