using MAEMS.Application.DTOs.Applicant;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Applicants.Queries.GetMyApplicant;

public class GetMyApplicantQuery : IRequest<BaseResponse<MyApplicantDto>>
{
    public int UserId { get; set; }
    public GetMyApplicantQuery(int userId) => UserId = userId;
}
