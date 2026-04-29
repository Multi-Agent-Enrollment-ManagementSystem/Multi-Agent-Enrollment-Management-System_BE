using MAEMS.Domain.Common;
using MAEMS.Domain.Entities;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.RegisterEvents.Commands.CreateRegisterEvent;

public class CreateRegisterEventCommandHandler : IRequestHandler<CreateRegisterEventCommand, BaseResponse<RegisterEvent>>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateRegisterEventCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<RegisterEvent>> Handle(CreateRegisterEventCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var article = await _unitOfWork.Articles.GetByIdAsync(request.ArticleId);
            if (article == null)
            {
                 return BaseResponse<RegisterEvent>.FailureResponse("Article not found", new List<string> { "The specified article does not exist" });
            }

            if (article.IsRegisterable != true)
            {
                 return BaseResponse<RegisterEvent>.FailureResponse("Not registerable", new List<string> { "This article is not open for registration" });
            }

            var eventRegistration = new RegisterEvent
            {
                ArticleId = request.ArticleId,
                FullName = request.FullName,
                Email = request.Email,
                Phone = request.Phone,
                CreatedAt = DateTime.Now
            };

            await _unitOfWork.RegisterEvents.AddAsync(eventRegistration);
            await _unitOfWork.SaveChangesAsync();

            return BaseResponse<RegisterEvent>.SuccessResponse(eventRegistration, "Registration submitted successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<RegisterEvent>.FailureResponse("Error submitting registration", new List<string> { ex.Message });
        }
    }
}