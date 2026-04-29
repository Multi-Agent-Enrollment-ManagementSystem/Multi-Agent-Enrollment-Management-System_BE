using AutoMapper;
using MAEMS.Application.DTOs.Application;
using MAEMS.Application.DTOs.Common;
using MAEMS.Application.Features.Applications.Queries.GetAllFullApplications;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Applications.Queries.GetAllFullApplications;

public class GetAllFullApplicationsQueryHandler : IRequestHandler<GetAllFullApplicationsQuery, BaseResponse<PagedResponse<FullApplicationDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAllFullApplicationsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<PagedResponse<FullApplicationDto>>> Handle(GetAllFullApplicationsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var (items, totalCount) = await _unitOfWork.Applications.GetApplicationsPagedAsync(
                request.ProgramId,
                request.CampusId,
                request.AdmissionTypeId,
                request.Status,
                request.RequiresReview,
                request.AssignedOfficerId,
                request.Level,
                request.Search,
                request.SortBy,
                request.SortDesc,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            var dtos = _mapper.Map<List<FullApplicationDto>>(items);

            var paged = new PagedResponse<FullApplicationDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber,
                PageSize = request.PageSize
            };

            return BaseResponse<PagedResponse<FullApplicationDto>>.SuccessResponse(paged, "Applications retrieved successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<PagedResponse<FullApplicationDto>>.FailureResponse(
                "Error retrieving applications",
                new List<string> { ex.Message }
            );
        }
    }
}