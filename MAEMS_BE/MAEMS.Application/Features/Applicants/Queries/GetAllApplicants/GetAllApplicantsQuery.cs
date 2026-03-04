using MAEMS.Application.DTOs.Applicant;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Applicants.Queries.GetAllApplicants;

public class GetAllApplicantsQuery : IRequest<BaseResponse<List<ApplicantDto>>> { }