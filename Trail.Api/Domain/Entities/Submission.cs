using Trail.Api.Domain.Enums;

namespace Trail.Api.Domain.Entities;

public class Submission
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public Guid ChallengeId { get; set; }
    public string DeliveryUrl { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;

    // Avaliação — embutida na Submission (decisão intencional do MVP)
    public Guid? ReviewerId { get; set; }
    public int? Score { get; set; }
    public string? Feedback { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public User Student { get; set; } = null!;
    public Challenge Challenge { get; set; } = null!;
    public User? Reviewer { get; set; }
}
