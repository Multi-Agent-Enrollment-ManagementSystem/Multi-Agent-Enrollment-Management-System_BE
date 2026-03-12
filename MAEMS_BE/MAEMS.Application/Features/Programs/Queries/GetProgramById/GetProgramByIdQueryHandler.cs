using AutoMapper;
using MAEMS.Application.DTOs.Program;
using MAEMS.Domain.Common;
using MAEMS.Domain.Interfaces;
using MediatR;

namespace MAEMS.Application.Features.Programs.Queries.GetProgramById;

public class GetProgramByIdQueryHandler : IRequestHandler<GetProgramByIdQuery, BaseResponse<ProgramDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetProgramByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BaseResponse<ProgramDto>> Handle(GetProgramByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var program = await _unitOfWork.Programs.GetByIdAsync(request.Id);

            if (program == null)
            {
                return BaseResponse<ProgramDto>.FailureResponse(
                    $"Program with ID {request.Id} not found",
                    new List<string> { "Program not found" }
                );
            }

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

            // Get all configs for this program and group by campus using AutoMapper
            var configs = await _unitOfWork.ProgramAdmissionConfigs.GetConfigsByProgramIdAsync(request.Id);

            programDto.Campuses = configs
                .GroupBy(c => new { c.CampusId, c.CampusName })
                .Select(g => new ProgramCampusDto
                {
                    CampusId = g.Key.CampusId,
                    CampusName = g.Key.CampusName,
                    Admissions = _mapper.Map<List<ProgramCampusAdmissionDto>>(g.ToList())
                })
                .ToList();

            return BaseResponse<ProgramDto>.SuccessResponse(programDto, "Program retrieved successfully");
        }
        catch (Exception ex)
        {
            return BaseResponse<ProgramDto>.FailureResponse(
                "Error retrieving program",
                new List<string> { ex.Message }
            );
        }
    }
}
