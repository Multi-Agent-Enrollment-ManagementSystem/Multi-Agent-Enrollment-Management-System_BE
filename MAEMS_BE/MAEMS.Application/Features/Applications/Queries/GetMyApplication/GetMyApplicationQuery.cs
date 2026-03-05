using MAEMS.Application.DTOs.Application;
using MAEMS.Domain.Common;
using MediatR;

namespace MAEMS.Application.Features.Applications.Queries.GetMyApplication;

public class GetMyApplicationQuery : IRequest<BaseResponse<MyApplicationDto>>
{
    public int UserId { get; set; }
    public GetMyApplicationQuery(int userId) => UserId = userId;
}