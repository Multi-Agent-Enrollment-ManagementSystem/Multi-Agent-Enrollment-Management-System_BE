using MAEMS.Application.DTOs.Common;
using MAEMS.Application.DTOs.Feedback;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Feedback.Queries.GetAllFeedbacks;

public class GetAllFeedbacksQueryHandler : IRequestHandler<GetAllFeedbacksQuery, BaseResponse<PagedResponse<FeedbackDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllFeedbacksQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<PagedResponse<FeedbackDto>>> Handle(GetAllFeedbacksQuery request, CancellationToken cancellationToken)
    {
        var (feedbacks, totalCount) = await _unitOfWork.Feedbacks.GetPagedWithUserAsync(request.PageNumber, request.PageSize);

        var dtos = feedbacks.Select(f => new FeedbackDto
        {
            Id = f.Id,
            UserId = f.UserId,
            Username = f.User?.Username, // Relies on Domain model having a User navigation property
            Title = f.Title,
            Content = f.Content,
            CreatedAt = f.CreatedAt
        }).ToList();

        var pagedResponse = new PagedResponse<FeedbackDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        return BaseResponse<PagedResponse<FeedbackDto>>.SuccessResponse(pagedResponse);
    }
}