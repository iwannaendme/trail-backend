using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trail.Api.Application.Services;
using Trail.Api.DTOs.Auth;

namespace Trail.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(AuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<LoginResponse>> Register(RegisterRequest request)
    {
        var result = await authService.RegisterAsync(request);
        if (result is null)
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict",
                detail: "Email já cadastrado.");

        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        if (result is null)
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "Email ou senha inválidos.");

        return Ok(result);
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var id = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var email = User.FindFirstValue(JwtRegisteredClaimNames.Email);
        var name = User.FindFirstValue("name");
        var role = User.FindFirstValue(ClaimTypes.Role);

        return Ok(new { id, email, name, role });
    }
}
