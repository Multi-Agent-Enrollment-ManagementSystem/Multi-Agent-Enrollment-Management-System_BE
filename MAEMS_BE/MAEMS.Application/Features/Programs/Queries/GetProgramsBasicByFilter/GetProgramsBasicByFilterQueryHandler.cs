using AutoMapper;
using MAEMS.Application.DTOs.Common;
using MAEMS.Application.DTOs.Program;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Programs.Queries.GetProgramsBasicByFilter;

public class GetProgramsBasicByFilterQueryHandler : IRequestHandler<GetProgramsBasicByFilterQuery, BaseResponse<PagedResponse<ProgramBasicDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetProgramsBasicByFilterQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<PagedResponse<ProgramBasicDto>>> Handle(GetProgramsBasicByFilterQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var (items, totalCount) = await _unitOfWork.Programs.GetProgramsBasicByFilterPagedAsync(
                request.MajorId,
                request.SearchName,
                request.SortBy,
                request.SortDesc,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            // Map domain -> dto. MajorName is not in domain; use MajorId lookup only if needed.
            // Current domain Program doesn't carry Major navigation, so we do lightweight mapping here.
            var dtos = new List<ProgramBasicDto>(items.Count);
            foreach (var program in items)
            {
                var dto = _mapper.Map<ProgramBasicDto>(program);

                if (program.MajorId.HasValue)
                {
                    var major = await _unitOfWork.Majors.GetByIdAsync(program.MajorId.Value);
                    dto.MajorName = major?.MajorName ?? string.Empty;
                }

                dtos.Add(dto);
            }

            var paged = new PagedResponse<ProgramBasicDto>
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber,
                PageSize = request.PageSize
            };

            return BaseResponse<PagedResponse<ProgramBasicDto>>.SuccessResponse(
                paged,
                $"Programs retrieved successfully. Found {totalCount} program(s)."
            );
        }
        catch (Exception ex)
        {
            return BaseResponse<PagedResponse<ProgramBasicDto>>.FailureResponse(
                "Error retrieving programs",
                new List<string> { ex.Message }
            );
        }
    }
}
