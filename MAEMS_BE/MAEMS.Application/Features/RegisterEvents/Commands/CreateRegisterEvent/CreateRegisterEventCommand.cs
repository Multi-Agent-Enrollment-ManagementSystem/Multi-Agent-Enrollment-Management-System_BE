using System.Text.Json.Serialization;
using MAEMS.Domain.Common;
using MAEMS.Domain.Entities;
using MediatR;

namespace MAEMS.Application.Features.RegisterEvents.Commands.CreateRegisterEvent;

public class CreateRegisterEventCommand : IRequest<BaseResponse<RegisterEvent>>
{
    public int ArticleId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}