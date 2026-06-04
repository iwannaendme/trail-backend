using System.ComponentModel.DataAnnotations;

namespace Trail.Api.DTOs.Trails;

public record CreateTrailRequest(
    [Required][MinLength(2)][MaxLength(256)] string Name,
    [Required][MaxLength(2000)] string Description
);
