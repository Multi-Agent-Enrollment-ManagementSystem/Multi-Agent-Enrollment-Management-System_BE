using MAEMS.Domain.Common;
using MediatR;
using System.Text.Json.Serialization;

namespace MAEMS.Application.Features.Feedback.Commands.SubmitFeedback;

public class SubmitFeedbackCommand : IRequest<BaseResponse<int>>
{
    [JsonIgnore]
    public int? UserId { get; set; }
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
}