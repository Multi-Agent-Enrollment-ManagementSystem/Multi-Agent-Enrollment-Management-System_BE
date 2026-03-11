using MAEMS.Application.DTOs.Application;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Applications.Commands.CreateApplication;

public class CreateApplicationCommand : IRequest<BaseResponse<ApplicationDto>>
{
    public int ApplicantId { get; set; }
    public int ConfigId { get; set; }
}