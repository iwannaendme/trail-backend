using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trail.Api.Application.Services;
using Trail.Api.DTOs.Trails;

namespace Trail.Api.Controllers;

[ApiController]
[Route("trails")]
[Authorize]
public class TrailsController(TrailService trailService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TrailResponse>>> List(CancellationToken ct)
        => Ok(await trailService.ListAsync(ct));

    [HttpGet("{id:guid}/challenges")]
    public async Task<ActionResult<IReadOnlyList<ChallengeResponse>>> GetChallenges(Guid id, CancellationToken ct)
    {
        var challenges = await trailService.GetChallengesAsync(id, ct);
        if (challenges is null)
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Not Found",
                detail: $"Trilha {id} não encontrada.");

        return Ok(challenges);
    }
}
