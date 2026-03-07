using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Applications.Commands.UpdateApplicationDecision;

public class UpdateApplicationDecisionCommand : IRequest<BaseResponse<bool>>
{
    public int ApplicationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool RequiresReview { get; set; }
    public int OfficerId { get; set; }
}