namespace Trail.Api.DTOs.Trails;

/// <summary>Returned after enrolling in or checking enrollment for a trail.</summary>
public record EnrollmentResponse(
    Guid Id,
    Guid TrailId,
    Guid StudentId,
    DateTime EnrolledAt,
    DateTime? CompletedAt
);
