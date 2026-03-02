using AutoMapper;
using MAEMS.Application.DTOs.Program;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Programs.Queries.GetProgramsBasicByFilter;

public class GetProgramsBasicByFilterQueryHandler : IRequestHandler<GetProgramsBasicByFilterQuery, BaseResponse<IEnumerable<ProgramBasicDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetProgramsBasicByFilterQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<IEnumerable<ProgramBasicDto>>> Handle(GetProgramsBasicByFilterQuery request, CancellationToken cancellationToken)
    {
        try
        {
            IEnumerable<MAEMS.Domain.Entities.Program> programs;

            // Filter by MajorId if provided
            if (request.MajorId.HasValue)
            {
                programs = await _unitOfWork.Programs.GetProgramsByMajorIdAsync(request.MajorId.Value);
                // Filter only active programs
                programs = programs.Where(p => p.IsActive == true);
            }
            else
            {
                // Get all active programs if no major filter
                programs = await _unitOfWork.Programs.GetActiveProgramsAsync();
            }

            // Filter by SearchName if provided
            if (!string.IsNullOrWhiteSpace(request.SearchName))
            {
                programs = programs.Where(p => 
                    p.ProgramName.Contains(request.SearchName, StringComparison.OrdinalIgnoreCase));
            }

            var programDtos = new List<ProgramBasicDto>();

            foreach (var program in programs)
            {
                var programDto = _mapper.Map<ProgramBasicDto>(program);
                
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

            return BaseResponse<IEnumerable<ProgramBasicDto>>.SuccessResponse(
                programDtos, 
                $"Programs retrieved successfully. Found {programDtos.Count} program(s).");
        }
        catch (Exception ex)
        {
            return BaseResponse<IEnumerable<ProgramBasicDto>>.FailureResponse(
                "Error retrieving programs",
                new List<string> { ex.Message }
            );
        }
    }
}
