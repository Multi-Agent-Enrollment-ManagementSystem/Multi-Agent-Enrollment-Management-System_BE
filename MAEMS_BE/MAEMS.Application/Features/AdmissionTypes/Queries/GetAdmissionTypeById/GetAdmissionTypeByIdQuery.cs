using MAEMS.Application.DTOs.AdmissionType;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.AdmissionTypes.Queries.GetAdmissionTypeById;

public record GetAdmissionTypeByIdQuery(int Id) : IRequest<BaseResponse<AdmissionTypeDto>>;
