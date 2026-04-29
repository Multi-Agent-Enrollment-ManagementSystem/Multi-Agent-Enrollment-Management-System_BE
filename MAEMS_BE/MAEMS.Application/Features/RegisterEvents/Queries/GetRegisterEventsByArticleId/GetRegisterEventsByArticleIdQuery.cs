using MAEMS.Domain.Common;
using MAEMS.Domain.Entities;
using MediatR;

namespace MAEMS.Application.Features.RegisterEvents.Queries.GetRegisterEventsByArticleId;

public class GetRegisterEventsByArticleIdQuery : IRequest<BaseResponse<IEnumerable<RegisterEvent>>>
{
    public int ArticleId { get; set; }
}