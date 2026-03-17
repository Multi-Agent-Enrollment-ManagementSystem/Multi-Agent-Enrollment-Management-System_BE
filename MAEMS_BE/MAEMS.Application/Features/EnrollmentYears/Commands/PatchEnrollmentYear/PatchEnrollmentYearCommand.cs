using MAEMS.Application.DTOs.EnrollmentYear;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.EnrollmentYears.Commands.PatchEnrollmentYear;

public class PatchEnrollmentYearCommand : IRequest<BaseResponse<EnrollmentYearDto>>
{
    public int EnrollmentYearId { get; set; }

    public string? Year { get; set; }
    public DateOnly? RegistrationStartDate { get; set; }
    public DateOnly? RegistrationEndDate { get; set; }
    public string? Status { get; set; }
}
