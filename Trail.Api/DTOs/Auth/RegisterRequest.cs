using System.ComponentModel.DataAnnotations;
using Trail.Api.Domain.Enums;

namespace Trail.Api.DTOs.Auth;

public record RegisterRequest(
    [Required][MinLength(2)][MaxLength(256)] string Name,
    [Required][EmailAddress][MaxLength(256)] string Email,
    [Required][MinLength(8)] string Password,
    [Required] UserRole Role
);
