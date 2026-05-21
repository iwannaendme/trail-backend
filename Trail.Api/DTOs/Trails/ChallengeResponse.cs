namespace Trail.Api.DTOs.Trails;

public record ChallengeResponse(Guid Id, Guid TrailId, string Title, string Description, int Order, DateTime CreatedAt);
