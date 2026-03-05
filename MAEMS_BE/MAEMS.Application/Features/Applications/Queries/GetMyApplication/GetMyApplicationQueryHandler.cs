using AutoMapper;
using MAEMS.Application.DTOs.Application;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Applications.Queries.GetMyApplication;

public class GetMyApplicationQueryHandler : IRequestHandler<GetMyApplicationQuery, BaseResponse<MyApplicationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetMyApplicationQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<MyApplicationDto>> Handle(GetMyApplicationQuery request, CancellationToken cancellationToken)
    {
        // Tìm applicant theo userId
        var applicant = await _unitOfWork.Applicants.GetByUserIdAsync(request.UserId);
        if (applicant == null)
        {
            return BaseResponse<MyApplicationDto>.FailureResponse("Applicant not found", new() { "No applicant profile found for this user" });
        }

        // Tìm application của applicant
        var application = await _unitOfWork.Applications.GetByApplicantIdAsync(applicant.ApplicantId);
        if (application == null)
        {
            return BaseResponse<MyApplicationDto>.FailureResponse("Application not found", new() { "No application found for this applicant" });
        }

        var dto = _mapper.Map<MyApplicationDto>(application);
        dto.ApplicantName = applicant.FullName; // Nếu cần lấy từ entity Applicant

        return BaseResponse<MyApplicationDto>.SuccessResponse(dto, "Application retrieved successfully");
    }
}