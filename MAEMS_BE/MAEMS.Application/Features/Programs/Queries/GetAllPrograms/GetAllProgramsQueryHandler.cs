using AutoMapper;
using MAEMS.Application.DTOs.Common;
using MAEMS.Application.DTOs.Program;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Programs.Queries.GetAllPrograms;

public class GetAllProgramsQueryHandler : IRequestHandler<GetAllProgramsQuery, BaseResponse<PagedResponse<ProgramDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAllProgramsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<PagedResponse<ProgramDto>>> Handle(GetAllProgramsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var (programs, totalCount) = await _unitOfWork.Programs.GetProgramsPagedAsync(
                request.MajorId,
                request.EnrollmentYearId,
                request.Search,
                request.SortBy,
                request.SortDesc,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            var programDtos = new List<ProgramDto>(programs.Count);

            foreach (var program in programs)
            {
                var programDto = _mapper.Map<ProgramDto>(program);

                if (program.MajorId.HasValue)
                {
                    var major = await _unitOfWork.Majors.GetByIdAsync(program.MajorId.Value);
                    programDto.MajorName = major?.MajorName ?? string.Empty;
                }
                else
                {
                    programDto.MajorName = string.Empty;
                }

                programDtos.Add(programDto);
            }

            var paged = new PagedResponse<ProgramDto>
            {
                Items = programDtos,
                TotalCount = totalCount,
                PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber,
                PageSize = request.PageSize
            };

            return BaseResponse<PagedResponse<ProgramDto>>.SuccessResponse(paged, "Programs retrieved successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<PagedResponse<ProgramDto>>.FailureResponse(
                "Error retrieving programs",
                new List<string> { ex.Message }
            );
        }
    }
}
