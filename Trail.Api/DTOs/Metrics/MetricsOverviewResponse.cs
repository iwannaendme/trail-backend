namespace Trail.Api.DTOs.Metrics;

public record MetricsOverviewResponse(
    int TotalStudents,
    int TotalTrails,
    int TotalChallenges,
    int TotalSubmissions,
    int PendingSubmissions,
    int ApprovedSubmissions,
    int NeedsRevisionSubmissions,
    decimal CompletionRate,
    decimal? ApprovalRate,          // Approved / (Approved + NeedsRevision) * 100. Null when no reviews exist.
    decimal? AverageLeadTimeHours   // Average hours from SubmittedAt to ReviewedAt.
);
