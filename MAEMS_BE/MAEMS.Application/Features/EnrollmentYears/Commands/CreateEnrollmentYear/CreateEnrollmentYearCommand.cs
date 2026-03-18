using MAEMS.Application.DTOs.EnrollmentYear;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.EnrollmentYears.Commands.CreateEnrollmentYear;

public class CreateEnrollmentYearCommand : IRequest<BaseResponse<EnrollmentYearDto>>
{
    public string Year { get; set; } = string.Empty;
    public DateOnly? RegistrationStartDate { get; set; }
    public DateOnly? RegistrationEndDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
