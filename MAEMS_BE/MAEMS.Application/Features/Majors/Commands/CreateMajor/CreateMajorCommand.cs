using MAEMS.Application.DTOs.Major;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Majors.Commands.CreateMajor;

public class CreateMajorCommand : IRequest<BaseResponse<MajorDto>>
{
    public string MajorCode { get; set; } = string.Empty;
    public string MajorName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool? IsActive { get; set; } = true;
}
