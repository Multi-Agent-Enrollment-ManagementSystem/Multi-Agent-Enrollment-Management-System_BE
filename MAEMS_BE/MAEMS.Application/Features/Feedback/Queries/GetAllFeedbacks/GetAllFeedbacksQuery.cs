using MAEMS.Application.DTOs.Common;
using MAEMS.Application.DTOs.Feedback;
using MAEMS.Domain.Common;
using MediatR;
using System.Collections.Generic;

namespace MAEMS.Application.Features.Feedback.Queries.GetAllFeedbacks;

public class GetAllFeedbacksQuery : IRequest<BaseResponse<PagedResponse<FeedbackDto>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}