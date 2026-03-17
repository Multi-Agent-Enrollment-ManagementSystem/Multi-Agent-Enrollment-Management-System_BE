using MAEMS.Application.DTOs.Program;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Programs.Commands.CreateProgram;

public class CreateProgramCommand : IRequest<BaseResponse<ProgramDto>>
{
    public string ProgramName { get; set; } = string.Empty;
    public int? MajorId { get; set; }
    public int? EnrollmentYearId { get; set; }
    public string? Description { get; set; }
    public string? CareerProspects { get; set; }
    public string? Duration { get; set; }
    public bool? IsActive { get; set; } = true;
}
