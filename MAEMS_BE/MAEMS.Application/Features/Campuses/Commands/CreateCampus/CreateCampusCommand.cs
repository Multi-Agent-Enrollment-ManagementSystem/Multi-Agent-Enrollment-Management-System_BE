using MAEMS.Application.DTOs.Campus;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Campuses.Commands.CreateCampus;

public class CreateCampusCommand : IRequest<BaseResponse<CampusDto>>
{
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; } = true;
}
