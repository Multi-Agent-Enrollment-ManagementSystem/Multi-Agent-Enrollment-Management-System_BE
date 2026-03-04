using AutoMapper;
using MAEMS.Application.DTOs.Applicant;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Applicants.Queries.GetMyApplicant;

public class GetMyApplicantQueryHandler : IRequestHandler<GetMyApplicantQuery, BaseResponse<MyApplicantDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetMyApplicantQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<MyApplicantDto>> Handle(GetMyApplicantQuery request, CancellationToken cancellationToken)
    {
        var applicant = await _unitOfWork.Applicants.GetByUserIdAsync(request.UserId);
        if (applicant == null)
        {
            return BaseResponse<MyApplicantDto>.FailureResponse("Applicant not found", new() { "No applicant profile found for this user" });
        }

        var dto = _mapper.Map<MyApplicantDto>(applicant);
        return BaseResponse<MyApplicantDto>.SuccessResponse(dto, "Applicant profile retrieved successfully");
    }
}
