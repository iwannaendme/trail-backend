using System.ComponentModel.DataAnnotations;

namespace Trail.Api.DTOs.Ai;

// ── Onboarding & trail generation ────────────────────────────────────────────

public record OnboardingProfileRequest(
    [Required][MaxLength(256)] string TargetRole,
    [Required][MaxLength(32)] string TechnicalDepth,   // beginner | intermediate | advanced
    [Required][MaxLength(16)] string WeeklyHours,      // <5 | 5-10 | 10-20 | 20+
    [Required][MaxLength(32)] string LearningStyle,    // hands-on | visual | theoretical | mixed
    [Required][MaxLength(1000)] string ProjectGoal
);

public record GenerateTrailResult(Guid TrailId, string Title);

/// <summary>Internal DTO matching the Anthropic tool output schema.</summary>
public record GeneratedTrailDto(
    string Title,
    string Description,
    decimal EstimatedHours,
    IReadOnlyList<GeneratedChallengeDto> Challenges);

public record GeneratedChallengeDto(
    string Title,
    string Description,
    IReadOnlyList<string> SearchTerms);

// ── Socratic chat ─────────────────────────────────────────────────────────────

public record ChatHistoryMessage(string Role, string Content);  // role: "user" | "assistant"

public record SocraticChatRequest(
    [Required] Guid ChallengeId,
    IReadOnlyList<ChatHistoryMessage> History,
    [Required][MaxLength(2000)] string NewMessage);

public record SocraticChatResponse(string Reply);

// ── AI code review ────────────────────────────────────────────────────────────

/// <summary>Structured draft returned to the mentor for editing before submission.</summary>
public record AiReviewDraft(
    string QualityAnalysis,
    IReadOnlyList<string> EdgeCases,
    string SuggestedComment,
    string SuggestedDecision);    // "Approved" | "NeedsRevision"
