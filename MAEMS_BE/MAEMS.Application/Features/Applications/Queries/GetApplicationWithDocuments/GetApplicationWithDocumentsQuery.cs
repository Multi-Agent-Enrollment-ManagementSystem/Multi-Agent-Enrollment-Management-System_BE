using MAEMS.Application.DTOs.Application;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Applications.Queries.GetApplicationWithDocuments;

public class GetApplicationWithDocumentsQuery : IRequest<BaseResponse<ApplicationWithDocumentsDto>>
{
    public int ApplicationId { get; set; }
    public GetApplicationWithDocumentsQuery(int applicationId) => ApplicationId = applicationId;
}