using MediatR;
using MAEMS.Application.DTOs.Score;
using MAEMS.Domain.Common;

namespace MAEMS.Application.Features.Scores.Queries.GetScoreByApplicantId;

public class GetScoreByApplicantIdQuery : IRequest<BaseResponse<ScoreDto>>
{
    public int ApplicantId { get; set; }

    public GetScoreByApplicantIdQuery(int applicantId)
    {
        ApplicantId = applicantId;
    }
}
