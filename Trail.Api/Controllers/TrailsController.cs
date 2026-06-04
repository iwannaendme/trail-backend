using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trail.Api.Application.Services;
using Trail.Api.DTOs.Trails;

namespace Trail.Api.Controllers;

[ApiController]
[Route("trails")]
[Authorize(Roles = "Student,Mentor,Manager")]
public class TrailsController(TrailService trailService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TrailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<TrailResponse>>> List([FromQuery] TrailListQuery query, CancellationToken ct)
        => Ok(await trailService.ListAsync(query, ct));

    [HttpGet("{id:guid}/challenges")]
    [ProducesResponseType(typeof(IReadOnlyList<ChallengeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
