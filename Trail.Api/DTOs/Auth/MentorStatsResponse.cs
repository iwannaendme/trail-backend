namespace Trail.Api.DTOs.Auth;

public record MentorStatsResponse(
    int ReviewsDone,
    int Approved,
    int NeedsRevision,
    int PendingInQueue
);
