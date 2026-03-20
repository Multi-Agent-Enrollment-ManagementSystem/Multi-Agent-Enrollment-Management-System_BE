using System.Text.Json.Serialization;
using MAEMS.Application.DTOs.Article;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Articles.Commands.CreateArticle;

public class CreateArticleCommand : IRequest<BaseResponse<ArticleDto>>
{
    [JsonIgnore]
    public int AuthorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Thumbnail { get; set; }
    public string? Status { get; set; }
}
