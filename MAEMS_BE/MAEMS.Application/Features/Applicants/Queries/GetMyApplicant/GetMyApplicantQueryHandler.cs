using AutoMapper;
using MAEMS.Application.DTOs.Applicant;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Applicants.Queries.GetMyApplicant;

public class GetMyApplicantQueryHandler : IRequestHandler<GetMyApplicantQuery, BaseResponse<ApplicantDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetMyApplicantQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<ApplicantDto>> Handle(GetMyApplicantQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var applicant = await _unitOfWork.Applicants.GetByUserIdAsync(request.UserId);

            if (applicant == null)
            {
                return BaseResponse<ApplicantDto>.FailureResponse(
                    "Applicant not found",
                    new List<string> { "No applicant profile found for this user" }
                );
            }

            var applicantDto = _mapper.Map<ApplicantDto>(applicant);
            
            return BaseResponse<ApplicantDto>.SuccessResponse(applicantDto, "Applicant retrieved successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<ApplicantDto>.FailureResponse(
                "Error retrieving applicant",
                new List<string> { ex.Message }
            );
        }
    }
}
