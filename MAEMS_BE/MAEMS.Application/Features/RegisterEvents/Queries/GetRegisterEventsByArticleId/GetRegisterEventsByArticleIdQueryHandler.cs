using MAEMS.Domain.Common;
using MAEMS.Domain.Entities;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.RegisterEvents.Queries.GetRegisterEventsByArticleId;

public class GetRegisterEventsByArticleIdQueryHandler : IRequestHandler<GetRegisterEventsByArticleIdQuery, BaseResponse<IEnumerable<RegisterEvent>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetRegisterEventsByArticleIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<IEnumerable<RegisterEvent>>> Handle(GetRegisterEventsByArticleIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var article = await _unitOfWork.Articles.GetByIdAsync(request.ArticleId);
            if (article == null)
            {
                return BaseResponse<IEnumerable<RegisterEvent>>.FailureResponse(
                    "Article not found", 
                    new List<string> { "The specified article does not exist" });
            }

            var registerEvents = await _unitOfWork.RegisterEvents.GetByArticleIdAsync(request.ArticleId);

            return BaseResponse<IEnumerable<RegisterEvent>>.SuccessResponse(
                registerEvents, 
                "Register events retrieved successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<IEnumerable<RegisterEvent>>.FailureResponse(
                "Error retrieving register events", 
                new List<string> { ex.Message });
        }
    }
}