using MAEMS.Application.DTOs.Application;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Applications.Queries.GetMyApplicationWithDocuments;

public class GetMyApplicationWithDocumentsQuery : IRequest<BaseResponse<List<ApplicationWithDocumentsDto>>>
{
    public int UserId { get; set; }
    public GetMyApplicationWithDocumentsQuery(int userId) => UserId = userId;
}