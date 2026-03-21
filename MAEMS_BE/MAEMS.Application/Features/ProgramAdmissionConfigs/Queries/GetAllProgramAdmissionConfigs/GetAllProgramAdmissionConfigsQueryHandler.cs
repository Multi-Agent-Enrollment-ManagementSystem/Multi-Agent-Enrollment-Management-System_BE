using AutoMapper;
using MAEMS.Application.DTOs.Common;
using MAEMS.Application.DTOs.ProgramAdmissionConfig;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.ProgramAdmissionConfigs.Queries.GetAllProgramAdmissionConfigs;

public class GetAllProgramAdmissionConfigsQueryHandler
    : IRequestHandler<GetAllProgramAdmissionConfigsQuery, BaseResponse<PagedResponse<ProgramAdmissionConfigDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAllProgramAdmissionConfigsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<PagedResponse<ProgramAdmissionConfigDto>>> Handle(
        GetAllProgramAdmissionConfigsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var (items, totalCount) = await _unitOfWork.ProgramAdmissionConfigs.GetConfigsPagedAsync(
                request.ProgramId,
                request.CampusId,
                request.AdmissionTypeId,
                request.Search,
                request.SortBy,
                request.SortDesc,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            var dtos = _mapper.Map<List<ProgramAdmissionConfigDto>>(items);

            var paged = new PagedResponse<ProgramAdmissionConfigDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber,
                PageSize = request.PageSize
            };

            return BaseResponse<PagedResponse<ProgramAdmissionConfigDto>>.SuccessResponse(
                paged,
                $"Program admission configs retrieved successfully. Found {totalCount} config(s).");
        }
        catch (Exception ex)
        {
            return BaseResponse<PagedResponse<ProgramAdmissionConfigDto>>.FailureResponse(
                "Error retrieving program admission configs",
                new List<string> { ex.Message });
        }
    }
}
