using MAEMS.Application.DTOs.Major;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Majors.Commands.PatchMajor;

public class PatchMajorCommand : IRequest<BaseResponse<MajorDto>>
{
    public int MajorId { get; set; }

    public string? MajorCode { get; set; }
    public string? MajorName { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}
