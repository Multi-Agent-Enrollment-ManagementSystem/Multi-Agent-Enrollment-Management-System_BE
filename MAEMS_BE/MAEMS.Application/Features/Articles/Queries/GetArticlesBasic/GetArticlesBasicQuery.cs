using MAEMS.Application.DTOs.Article;
using MAEMS.Application.DTOs.Common;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Articles.Queries.GetArticlesBasic;

public class GetArticlesBasicQuery : IRequest<BaseResponse<PagedResponse<ArticleBasicDto>>>
{
    public string? SearchTitle { get; set; }
    public string? Status { get; set; } // optional filter

    public string? SortBy { get; set; } = "updatedAt"; // updatedAt | title | articleId | status
    public bool SortDesc { get; set; } = true;

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
