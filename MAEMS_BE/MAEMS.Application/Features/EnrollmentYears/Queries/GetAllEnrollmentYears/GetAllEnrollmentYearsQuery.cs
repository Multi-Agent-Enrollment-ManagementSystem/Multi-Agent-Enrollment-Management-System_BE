using MAEMS.Application.DTOs.EnrollmentYear;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.EnrollmentYears.Queries.GetAllEnrollmentYears;

public record GetAllEnrollmentYearsQuery : IRequest<BaseResponse<IEnumerable<EnrollmentYearDto>>>;
