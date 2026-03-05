using MAEMS.Application.DTOs.Application;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Applications.Queries.GetMyApplications;

public class GetMyApplicationsQuery : IRequest<BaseResponse<List<MyApplicationDto>>>
{
    public int UserId { get; set; }
    public GetMyApplicationsQuery(int userId) => UserId = userId;
}