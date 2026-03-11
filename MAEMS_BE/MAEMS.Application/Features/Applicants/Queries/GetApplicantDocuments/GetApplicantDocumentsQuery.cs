using MAEMS.Application.DTOs.Document;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Applicants.Queries.GetApplicantDocuments;

public class GetApplicantDocumentsQuery : IRequest<BaseResponse<List<DocumentDto>>>
{
    public int ApplicantId { get; set; }
    public GetApplicantDocumentsQuery(int applicantId) => ApplicantId = applicantId;
}