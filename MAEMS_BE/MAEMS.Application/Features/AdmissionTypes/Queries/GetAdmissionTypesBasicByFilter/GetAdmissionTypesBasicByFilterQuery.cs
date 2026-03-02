using MAEMS.Application.DTOs.AdmissionType;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.AdmissionTypes.Queries.GetAdmissionTypesBasicByFilter;

public record GetAdmissionTypesBasicByFilterQuery(int? EnrollmentYearId) : IRequest<BaseResponse<IEnumerable<AdmissionTypeBasicDto>>>;
