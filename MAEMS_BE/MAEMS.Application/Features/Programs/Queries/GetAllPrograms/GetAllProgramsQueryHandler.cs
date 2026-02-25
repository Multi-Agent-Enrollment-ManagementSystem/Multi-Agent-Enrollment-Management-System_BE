using AutoMapper;
using MAEMS.Application.DTOs.Program;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Programs.Queries.GetAllPrograms;

public class GetAllProgramsQueryHandler : IRequestHandler<GetAllProgramsQuery, BaseResponse<IEnumerable<ProgramDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAllProgramsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<IEnumerable<ProgramDto>>> Handle(GetAllProgramsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var programs = await _unitOfWork.Programs.GetAllAsync();
            var programDtos = new List<ProgramDto>();

            foreach (var program in programs)
            {
                var programDto = _mapper.Map<ProgramDto>(program);
                
                // Get major name if MajorId exists
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

            return BaseResponse<IEnumerable<ProgramDto>>.SuccessResponse(programDtos, "Programs retrieved successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<IEnumerable<ProgramDto>>.FailureResponse(
                "Error retrieving programs",
                new List<string> { ex.Message }
            );
        }
    }
}
