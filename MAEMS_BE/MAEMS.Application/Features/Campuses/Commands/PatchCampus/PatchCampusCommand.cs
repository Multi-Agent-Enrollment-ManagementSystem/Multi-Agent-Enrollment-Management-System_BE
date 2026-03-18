using MAEMS.Application.DTOs.Campus;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Campuses.Commands.PatchCampus;

public class PatchCampusCommand : IRequest<BaseResponse<CampusDto>>
{
    public int CampusId { get; set; }

    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}
