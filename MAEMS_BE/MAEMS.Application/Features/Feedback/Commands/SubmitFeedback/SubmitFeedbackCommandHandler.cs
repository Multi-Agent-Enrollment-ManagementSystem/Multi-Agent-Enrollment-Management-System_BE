using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Feedback.Commands.SubmitFeedback;

public class SubmitFeedbackCommandHandler : IRequestHandler<SubmitFeedbackCommand, BaseResponse<int>>
{
    private readonly IUnitOfWork _unitOfWork;

    public SubmitFeedbackCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<int>> Handle(SubmitFeedbackCommand request, CancellationToken cancellationToken)
    {
        var feedback = new Domain.Entities.Feedback
        {
            UserId = request.UserId,
            Title = request.Title,
            Content = request.Content,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _unitOfWork.Feedbacks.AddAsync(feedback);
        return BaseResponse<int>.SuccessResponse(result.Id, "Feedback submitted successfully.");
    }
}