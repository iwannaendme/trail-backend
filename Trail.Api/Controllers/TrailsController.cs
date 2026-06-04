using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
    // ── Queries (all authenticated roles) ─────────────────────────────────────

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TrailResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TrailResponse>>> List(
        [FromQuery] TrailListQuery query, CancellationToken ct)
        => Ok(await trailService.ListAsync(query, ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TrailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TrailResponse>> GetById(Guid id, CancellationToken ct)
    {
        var trail = await trailService.GetByIdAsync(id, ct);
        if (trail is null)
            return Problem(statusCode: 404, title: "Not Found", detail: $"Trilha {id} não encontrada.");
        return Ok(trail);
    }

    /// <summary>
    /// Returns challenges for a trail. When the caller is a Student, each challenge
    /// is enriched with that student's submission status (isCompleted, lastSubmissionAt, etc.).
    /// Mentors and Managers receive challenges without per-student context.
    /// </summary>
    [HttpGet("{id:guid}/challenges")]
    [ProducesResponseType(typeof(IReadOnlyList<ChallengeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ChallengeResponse>>> GetChallenges(
        Guid id, CancellationToken ct)
    {
        // Pass studentId so the service can reflect real completion status.
        // Mentors/Managers don't have a "personal" view, so their context is null.
        var studentId = IsStudent() ? GetUserId() : null;
        var challenges = await trailService.GetChallengesAsync(id, studentId, ct);

        if (challenges is null)
            return Problem(statusCode: 404, title: "Not Found", detail: $"Trilha {id} não encontrada.");

        return Ok(challenges);
    }

    // ── Enrollment (Student only) ─────────────────────────────────────────────

    [HttpPost("{id:guid}/enroll")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(EnrollmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EnrollmentResponse>> Enroll(Guid id, CancellationToken ct)
    {
        var studentId = GetUserId();
        if (studentId is null) return Unauthorized();

        var result = await trailService.EnrollAsync(id, studentId.Value, ct);
        if (result is null)
            return Problem(statusCode: 404, title: "Not Found", detail: $"Trilha {id} não encontrada.");

        return Ok(result);
    }

    [HttpGet("{id:guid}/enrollment")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> GetEnrollment(Guid id, CancellationToken ct)
    {
        var studentId = GetUserId();
        if (studentId is null) return Unauthorized();

        var enrolled = await trailService.IsEnrolledAsync(id, studentId.Value, ct);
        return Ok(new { enrolled });
    }

    // ── Mutations (Manager only) ───────────────────────────────────────────────

    [HttpPost]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(typeof(TrailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<TrailResponse>> Create(CreateTrailRequest request, CancellationToken ct)
    {
        var trail = await trailService.CreateAsync(request, ct);
        return Created($"/trails/{trail.Id}", trail);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(typeof(TrailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TrailResponse>> Update(Guid id, UpdateTrailRequest request, CancellationToken ct)
    {
        var trail = await trailService.UpdateAsync(id, request, ct);
        if (trail is null)
            return Problem(statusCode: 404, title: "Not Found", detail: $"Trilha {id} não encontrada.");
        return Ok(trail);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) =>
        await trailService.DeleteAsync(id, ct) is null
            ? Problem(statusCode: 404, title: "Not Found", detail: $"Trilha {id} não encontrada.")
            : NoContent();

    // ── Challenge mutations (Manager only) ────────────────────────────────────

    [HttpPost("{id:guid}/challenges")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(typeof(ChallengeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChallengeResponse>> AddChallenge(
        Guid id, CreateChallengeRequest request, CancellationToken ct)
    {
        var challenge = await trailService.AddChallengeAsync(id, request, ct);
        if (challenge is null)
            return Problem(statusCode: 404, title: "Not Found", detail: $"Trilha {id} não encontrada.");
        return Created($"/trails/{id}/challenges/{challenge.Id}", challenge);
    }

    [HttpPut("{id:guid}/challenges/{challengeId:guid}")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(typeof(ChallengeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChallengeResponse>> UpdateChallenge(
        Guid id, Guid challengeId, UpdateChallengeRequest request, CancellationToken ct)
    {
        var challenge = await trailService.UpdateChallengeAsync(id, challengeId, request, ct);
        if (challenge is null)
            return Problem(statusCode: 404, title: "Not Found",
                detail: $"Desafio {challengeId} não encontrado na trilha {id}.");
        return Ok(challenge);
    }

    [HttpDelete("{id:guid}/challenges/{challengeId:guid}")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteChallenge(Guid id, Guid challengeId, CancellationToken ct) =>
        await trailService.DeleteChallengeAsync(id, challengeId, ct) is null
            ? Problem(statusCode: 404, title: "Not Found",
                  detail: $"Desafio {challengeId} não encontrado na trilha {id}.")
            : NoContent();

    // ── Helpers ────────────────────────────────────────────────────────────────

    private Guid? GetUserId()
    {
        var value = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                 ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private bool IsStudent() =>
        User.IsInRole("Student");
}
