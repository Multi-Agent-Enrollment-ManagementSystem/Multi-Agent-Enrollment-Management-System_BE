using AutoMapper;
using MAEMS.Application.DTOs.Common;
using MAEMS.Application.DTOs.Major;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Majors.Queries.GetAllMajors;

public class GetAllMajorsQueryHandler : IRequestHandler<GetAllMajorsQuery, BaseResponse<PagedResponse<MajorDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAllMajorsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<PagedResponse<MajorDto>>> Handle(GetAllMajorsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var (items, totalCount) = await _unitOfWork.Majors.GetMajorsPagedAsync(
                request.Search,
                request.SortBy,
                request.SortDesc,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            var dtos = _mapper.Map<List<MajorDto>>(items);

            var paged = new PagedResponse<MajorDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber,
                PageSize = request.PageSize
            };

            return BaseResponse<PagedResponse<MajorDto>>.SuccessResponse(paged, "Majors retrieved successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<PagedResponse<MajorDto>>.FailureResponse(
                "Error retrieving majors",
                new List<string> { ex.Message }
            );
        }
    }
}
