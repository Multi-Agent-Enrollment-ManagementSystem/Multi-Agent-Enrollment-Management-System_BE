using MAEMS.Application.DTOs.Application;
using MAEMS.Domain.Common;
using MediatR;
using System.Text.Json.Serialization;

namespace MAEMS.Application.Features.Applications.Commands.SubmitApplication;

public class SubmitApplicationCommand : IRequest<BaseResponse<ApplicationDto>>
{
    public int ApplicationId { get; set; }

    [JsonIgnore]
    public int UserId { get; set; } // Will be set from JWT in Controller
}
