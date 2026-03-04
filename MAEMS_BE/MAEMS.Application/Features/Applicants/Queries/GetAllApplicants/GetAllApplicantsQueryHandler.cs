using AutoMapper;
using MAEMS.Application.DTOs.Applicant;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Applicants.Queries.GetAllApplicants;

public class GetAllApplicantsQueryHandler : IRequestHandler<GetAllApplicantsQuery, BaseResponse<List<ApplicantDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAllApplicantsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<List<ApplicantDto>>> Handle(GetAllApplicantsQuery request, CancellationToken cancellationToken)
    {
        var applicants = await _unitOfWork.Applicants.GetAllAsync();
        var dtos = _mapper.Map<List<ApplicantDto>>(applicants);
        return BaseResponse<List<ApplicantDto>>.SuccessResponse(dtos, "Applicants retrieved successfully");
    }
}