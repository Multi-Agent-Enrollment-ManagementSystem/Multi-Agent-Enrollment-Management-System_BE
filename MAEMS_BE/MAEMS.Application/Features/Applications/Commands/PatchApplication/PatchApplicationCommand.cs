using MAEMS.Application.DTOs.Application;
using MAEMS.Domain.Common;
using MediatR;
using System.Text.Json.Serialization;

namespace MAEMS.Application.Features.Applications.Commands.PatchApplication;

public class PatchApplicationCommand : IRequest<BaseResponse<ApplicationDto>>
{
    public int ApplicationId { get; set; }

    /// <summary>New status value. Null = no change.</summary>
    public string? Status { get; set; }

    /// <summary>Whether the application requires manual review. Null = no change.</summary>
    public bool? RequiresReview { get; set; }

    /// <summary>UserId of the officer performing the action — set from JWT in the controller.</summary>
    [JsonIgnore]
    public int OfficerUserId { get; set; }
}
