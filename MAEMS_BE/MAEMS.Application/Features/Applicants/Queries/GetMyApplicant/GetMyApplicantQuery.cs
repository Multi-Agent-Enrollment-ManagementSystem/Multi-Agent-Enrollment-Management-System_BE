using MAEMS.Application.DTOs.Applicant;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Applicants.Queries.GetMyApplicant;

public record GetMyApplicantQuery(int UserId) : IRequest<BaseResponse<ApplicantDto>>;
