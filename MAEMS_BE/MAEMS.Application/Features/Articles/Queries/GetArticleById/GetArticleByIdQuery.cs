using MAEMS.Application.DTOs.Article;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Articles.Queries.GetArticleById;

public class GetArticleByIdQuery : IRequest<BaseResponse<ArticleDto>>
{
    public int ArticleId { get; set; }
}
