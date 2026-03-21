using AutoMapper;
using MAEMS.Application.DTOs.Article;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Articles.Queries.GetArticleById;

public class GetArticleByIdQueryHandler : IRequestHandler<GetArticleByIdQuery, BaseResponse<ArticleDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetArticleByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<ArticleDto>> Handle(GetArticleByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var (article, authorname) = await _unitOfWork.Articles.GetArticleWithAuthornameByIdAsync(request.ArticleId, cancellationToken);
            if (article == null)
                return BaseResponse<ArticleDto>.FailureResponse("Article not found", new List<string> { "Article not found" });

            var dto = _mapper.Map<ArticleDto>(article);
            dto.Authorname = authorname;

            return BaseResponse<ArticleDto>.SuccessResponse(dto, "Article retrieved successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<ArticleDto>.FailureResponse("Error retrieving article", new List<string> { ex.Message });
        }
    }
}
