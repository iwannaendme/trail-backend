using Trail.Api.Domain.Enums;

namespace Trail.Api.DTOs.Auth;

public record RegisterRequest(string Name, string Email, string Password, UserRole Role);
