using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Applications.Commands.UpdateApplicationDecision;

public class UpdateApplicationDecisionCommandHandler : IRequestHandler<UpdateApplicationDecisionCommand, BaseResponse<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateApplicationDecisionCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<bool>> Handle(UpdateApplicationDecisionCommand request, CancellationToken cancellationToken)
    {
        var application = await _unitOfWork.Applications.GetByIdAsync(request.ApplicationId);
        if (application == null)
            return BaseResponse<bool>.FailureResponse("Application not found", new() { "Application does not exist" });

        application.Status = request.Status;
        application.RequiresReview = request.RequiresReview;
        application.AssignedOfficerId = request.OfficerId;
        application.LastUpdated = DateTime.UtcNow;

        await _unitOfWork.Applications.UpdateAsync(application);
        await _unitOfWork.SaveChangesAsync();

        return BaseResponse<bool>.SuccessResponse(true, "Application decision updated successfully");
    }
}