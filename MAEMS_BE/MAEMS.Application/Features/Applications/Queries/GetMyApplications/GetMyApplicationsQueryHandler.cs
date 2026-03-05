using AutoMapper;
using MAEMS.Application.DTOs.Application;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Applications.Queries.GetMyApplications;

public class GetMyApplicationsQueryHandler : IRequestHandler<GetMyApplicationsQuery, BaseResponse<List<MyApplicationDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetMyApplicationsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<List<MyApplicationDto>>> Handle(GetMyApplicationsQuery request, CancellationToken cancellationToken)
    {
        var applicant = await _unitOfWork.Applicants.GetByUserIdAsync(request.UserId);
        if (applicant == null)
        {
            return BaseResponse<List<MyApplicationDto>>.FailureResponse("Applicant not found", new() { "No applicant profile found for this user" });
        }

        var applications = await _unitOfWork.Applications.GetAllByApplicantIdAsync(applicant.ApplicantId);
        var dtos = _mapper.Map<List<MyApplicationDto>>(applications);

        return BaseResponse<List<MyApplicationDto>>.SuccessResponse(dtos, "Applications retrieved successfully");
    }
}