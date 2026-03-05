using AutoMapper;
using MAEMS.Application.DTOs.Application;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Applications.Queries.GetAllFullApplications;

public class GetAllFullApplicationsQueryHandler : IRequestHandler<GetAllFullApplicationsQuery, BaseResponse<List<FullApplicationDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAllFullApplicationsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<List<FullApplicationDto>>> Handle(GetAllFullApplicationsQuery request, CancellationToken cancellationToken)
    {
        var applications = await _unitOfWork.Applications.GetAllAsync();
        var dtos = _mapper.Map<List<FullApplicationDto>>(applications);
        return BaseResponse<List<FullApplicationDto>>.SuccessResponse(dtos, "Applications retrieved successfully");
    }
}