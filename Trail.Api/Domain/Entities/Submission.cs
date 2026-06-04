using Trail.Api.Domain.Enums;

namespace Trail.Api.Domain.Entities;

public class Submission
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid ChallengeId { get; set; }

    /// <summary>
    /// Validated to match a GitHub repository (github.com/user/repo)
    /// or Gist (gist.github.com/user/id) URL.
    /// </summary>
    public string GitHubUrl { get; set; } = string.Empty;

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;

    public Guid? ReviewerId { get; set; }

    /// <summary>
    /// Short actionable mentor note (max 500 chars). Replaces the 0–100 score
    /// system — binary decision (Approved/NeedsRevision) + brief context is more
    /// actionable than a number the mentor has to calibrate.
    /// </summary>
    public string? MentorComment { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public User Student { get; set; } = null!;
    public Challenge Challenge { get; set; } = null!;
    public User? Reviewer { get; set; }
}
