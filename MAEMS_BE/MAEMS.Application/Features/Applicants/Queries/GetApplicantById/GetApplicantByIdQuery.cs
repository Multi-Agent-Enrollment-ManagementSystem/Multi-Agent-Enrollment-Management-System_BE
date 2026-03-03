using MAEMS.Application.DTOs.Applicant;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Applicants.Queries.GetApplicantById;

public record GetApplicantByIdQuery(int ApplicantId) : IRequest<BaseResponse<ApplicantDto>>;
