using AutoMapper;
using MAEMS.Application.DTOs.Article;
using MAEMS.Application.DTOs.Common;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Articles.Queries.GetPublishedArticlesBasic;

public class GetPublishedArticlesBasicQueryHandler : IRequestHandler<GetPublishedArticlesBasicQuery, BaseResponse<PagedResponse<ArticleBasicDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetPublishedArticlesBasicQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<PagedResponse<ArticleBasicDto>>> Handle(GetPublishedArticlesBasicQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var (items, totalCount) = await _unitOfWork.Articles.GetPublishedArticlesBasicPagedAsync(
                request.SearchTitle,
                request.SortBy,
                request.SortDesc,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            var dtos = items.Select(_mapper.Map<ArticleBasicDto>).ToList();

            var paged = new PagedResponse<ArticleBasicDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber,
                PageSize = request.PageSize
            };

            return BaseResponse<PagedResponse<ArticleBasicDto>>.SuccessResponse(paged, $"Published articles retrieved successfully. Found {totalCount} article(s)." );
        }
        catch (Exception ex)
        {
            return BaseResponse<PagedResponse<ArticleBasicDto>>.FailureResponse("Error retrieving published articles", new List<string> { ex.Message });
        }
    }
}
