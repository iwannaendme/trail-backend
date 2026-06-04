namespace Trail.Api.DTOs.Submissions;

public record SubmissionResponse(
    Guid Id,
    Guid StudentId,
    string StudentName,
    Guid ChallengeId,
    string ChallengeTitle,
    string? TrailName,
    string GitHubUrl,
    DateTime SubmittedAt,
    string Status,          // "Submitted" | "Approved" | "NeedsRevision"
    Guid? ReviewerId,
    string? ReviewerName,
    string? MentorComment,
    DateTime? ReviewedAt
);
