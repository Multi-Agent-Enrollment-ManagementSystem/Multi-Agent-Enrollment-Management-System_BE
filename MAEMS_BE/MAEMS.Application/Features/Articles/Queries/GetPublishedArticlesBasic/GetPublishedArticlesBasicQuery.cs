using MAEMS.Application.DTOs.Article;
using MAEMS.Application.DTOs.Common;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Articles.Queries.GetPublishedArticlesBasic;

public class GetPublishedArticlesBasicQuery : IRequest<BaseResponse<PagedResponse<ArticleBasicDto>>>
{
    public string? SearchTitle { get; set; }

    public string? SortBy { get; set; } = "updatedAt"; // updatedAt | title | articleId
    public bool SortDesc { get; set; } = true;

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
