using MAEMS.Application.DTOs.Application;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Applications.Queries.GetMyApplicationWithDocuments;

public class GetMyApplicationWithDocumentsQuery : IRequest<BaseResponse<ApplicationWithDocumentsDto>>
{
    public int UserId { get; set; }
    public int ApplicationId { get; set; }
    public GetMyApplicationWithDocumentsQuery(int userId, int applicationId)
    {
        UserId = userId;
        ApplicationId = applicationId;
    }
}