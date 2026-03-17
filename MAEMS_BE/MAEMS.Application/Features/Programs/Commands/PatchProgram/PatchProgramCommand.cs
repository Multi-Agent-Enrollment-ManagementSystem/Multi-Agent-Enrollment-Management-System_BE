using MAEMS.Application.DTOs.Program;
using MAEMS.Domain.Common;
using MediatR;
using System.Text.Json.Serialization;

namespace MAEMS.Application.Features.Programs.Commands.PatchProgram;

public class PatchProgramCommand : IRequest<BaseResponse<ProgramDto>>
{
    public int ProgramId { get; set; }

    public string? ProgramName { get; set; }
    public string? Description { get; set; }
    public string? CareerProspects { get; set; }
    public string? Duration { get; set; }
    public bool? IsActive { get; set; }
}
