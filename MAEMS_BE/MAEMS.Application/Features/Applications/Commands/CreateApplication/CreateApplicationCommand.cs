using MAEMS.Application.DTOs.Application;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Applications.Commands.CreateApplication;

public class CreateApplicationCommand : IRequest<BaseResponse<ApplicationDto>>
{
    public int ApplicantId { get; set; }
    public int ProgramId { get; set; }
    public int EnrollmentYearId { get; set; }
    public int CampusId { get; set; }
    public int AdmissionTypeId { get; set; }
}