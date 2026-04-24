using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trail.Api.Infrastructure.Data;

namespace Trail.Api.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "Trail API is running", timestamp = DateTime.UtcNow });

    [HttpGet("detailed")]
    [Authorize(Roles = "Mentor,Manager")]
    public async Task<IActionResult> Detailed([FromServices] AppDbContext db)
    {
        var canConnect = await db.Database.CanConnectAsync();
        if (!canConnect)
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Service Unavailable",
                detail: "Database connection failed.");

        return Ok(new { status = "healthy", database = "connected", timestamp = DateTime.UtcNow });
    }
}
