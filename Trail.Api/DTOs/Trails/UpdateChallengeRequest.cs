using System.ComponentModel.DataAnnotations;

namespace Trail.Api.DTOs.Trails;

public record UpdateChallengeRequest(
    [Required][MinLength(2)][MaxLength(256)] string Title,
    [Required][MaxLength(2000)] string Description,
    [Range(1, int.MaxValue)] int Order,
    [Url][MaxLength(2048)] string? YouTubeUrl
);
