using MAEMS.Application.DTOs.Application;
using MAEMS.Domain.Common;
using MediatR;
using System.Text.Json.Serialization;

namespace MAEMS.Application.Features.Applications.Commands.RequestAdditionalDocs;

public sealed class RequestAdditionalDocsCommand : IRequest<BaseResponse<ApplicationDto>>
{
    [JsonIgnore]
    public int ApplicationId { get; set; }

    public string DocsNeed { get; set; } = string.Empty;

    [JsonIgnore]
    public int OfficerUserId { get; set; }
}
