namespace Trail.Api.DTOs.Auth;

public record LoginResponse(string Token, string Role, string Name);
