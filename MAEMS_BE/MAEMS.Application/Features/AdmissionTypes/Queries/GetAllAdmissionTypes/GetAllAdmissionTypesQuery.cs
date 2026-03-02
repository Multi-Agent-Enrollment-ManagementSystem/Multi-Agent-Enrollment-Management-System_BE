using MAEMS.Application.DTOs.AdmissionType;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.AdmissionTypes.Queries.GetAllAdmissionTypes;

public record GetAllAdmissionTypesQuery : IRequest<BaseResponse<IEnumerable<AdmissionTypeDto>>>;
