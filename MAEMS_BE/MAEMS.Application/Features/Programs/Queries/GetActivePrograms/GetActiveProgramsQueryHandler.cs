using AutoMapper;
using MAEMS.Application.DTOs.Program;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Programs.Queries.GetActivePrograms;

public class GetActiveProgramsQueryHandler : IRequestHandler<GetActiveProgramsQuery, BaseResponse<IEnumerable<ProgramDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetActiveProgramsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<IEnumerable<ProgramDto>>> Handle(GetActiveProgramsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var programs = await _unitOfWork.Programs.GetActiveProgramsAsync();
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

            return BaseResponse<IEnumerable<ProgramDto>>.SuccessResponse(programDtos, "Active programs retrieved successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<IEnumerable<ProgramDto>>.FailureResponse(
                "Error retrieving active programs",
                new List<string> { ex.Message }
            );
        }
    }
}
