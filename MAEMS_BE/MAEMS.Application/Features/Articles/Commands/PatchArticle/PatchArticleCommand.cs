using System.Text.Json.Serialization;
using MAEMS.Application.DTOs.Article;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Articles.Commands.PatchArticle;

public class PatchArticleCommand : IRequest<BaseResponse<ArticleDto>>
{
    [JsonIgnore]
    public int ArticleId { get; set; }

    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Thumbnail { get; set; }
    public string? Status { get; set; }
}
