using System.ComponentModel.DataAnnotations;

namespace Trail.Api.DTOs.Submissions;

/// <summary>Binary review decision — replaces the 0–100 score slider.</summary>
public enum ReviewDecision { Approved, NeedsRevision }

/// <summary>
/// Minimalist payload: one decision + optional short note.
/// No score calibration needed — the only judgment is "passes or doesn't".
/// </summary>
public record ReviewSubmissionRequest(
    [Required] ReviewDecision Decision,
    [MaxLength(500)] string? Comment
);
