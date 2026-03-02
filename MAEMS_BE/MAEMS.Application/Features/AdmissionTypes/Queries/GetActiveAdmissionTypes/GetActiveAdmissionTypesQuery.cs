using MAEMS.Application.DTOs.AdmissionType;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.AdmissionTypes.Queries.GetActiveAdmissionTypes;

public record GetActiveAdmissionTypesQuery : IRequest<BaseResponse<IEnumerable<AdmissionTypeDto>>>;
