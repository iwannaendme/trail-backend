namespace Trail.Api.Domain.Entities;

/// <summary>
/// Captures the onboarding data-points the AI uses to generate a personalised trail.
/// Stored once per student; updated on re-onboarding.
/// </summary>
public class StudentOnboardingProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }

    /// <summary>"Backend Developer with .NET", "Frontend React Engineer", etc.</summary>
    public string TargetRole { get; set; } = string.Empty;

    /// <summary>"beginner" | "intermediate" | "advanced"</summary>
    public string TechnicalDepth { get; set; } = string.Empty;

    /// <summary>"<5" | "5-10" | "10-20" | "20+"  (hours per week)</summary>
    public string WeeklyHours { get; set; } = string.Empty;

    /// <summary>"hands-on" | "visual" | "theoretical" | "mixed"</summary>
    public string LearningStyle { get; set; } = string.Empty;

    /// <summary>Free-text: "Build a REST API for a booking system", etc.</summary>
    public string ProjectGoal { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}
