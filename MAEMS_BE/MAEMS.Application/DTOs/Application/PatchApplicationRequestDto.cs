namespace MAEMS.Application.DTOs.Application;

/// <summary>
/// Request body for PATCH /api/applications/{id} (officer only).
/// Only <see cref="Status"/> and <see cref="RequiresReview"/> can be changed by an officer.
/// </summary>
public class PatchApplicationRequestDto
{
    /// <summary>New status value (e.g. "under_review", "approved", "rejected"). Null = no change.</summary>
    public string? Status { get; set; }

    /// <summary>Whether the application requires manual review. Null = no change.</summary>
    public bool? RequiresReview { get; set; }
}
