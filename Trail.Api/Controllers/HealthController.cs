using Microsoft.AspNetCore.Mvc;

namespace Trail.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "Trail API is running", timestamp = DateTime.UtcNow });
}
