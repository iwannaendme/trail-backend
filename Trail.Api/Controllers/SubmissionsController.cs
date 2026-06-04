using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trail.Api.Application.Services;
using Trail.Api.DTOs.Submissions;

namespace Trail.Api.Controllers;

[ApiController]
[Route("submissions")]
public class SubmissionsController(SubmissionService submissionService) : ControllerBase
{
    /// <summary>Student submits a GitHub repo or Gist link for a challenge.</summary>
    [HttpPost]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(SubmissionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubmissionResponse>> Create(
        CreateSubmissionRequest request, CancellationToken ct)
    {
        var studentId = GetUserId();
        if (studentId is null) return Unauthorized();

        var result = await submissionService.CreateAsync(studentId.Value, request, ct);
        if (result is null)
            return Problem(statusCode: 404, title: "Not Found",
                detail: "Challenge not found or user is not a student.");

        return Created($"/submissions/{result.Id}", result);
    }

    /// <summary>
    /// Mentor/Manager view: all submissions awaiting review, oldest-first.
    /// Each entry includes trail name so the reviewer has context without clicking through.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Mentor,Manager")]
    [ProducesResponseType(typeof(IReadOnlyList<SubmissionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SubmissionResponse>>> ListPending(CancellationToken ct)
        => Ok(await submissionService.ListPendingAsync(ct));

    /// <summary>
    /// Lightweight badge endpoint: returns the count of pending submissions.
    /// Used by the sidebar badge without fetching full submission data.
    /// </summary>
    [HttpGet("pending/count")]
    [Authorize(Roles = "Mentor,Manager")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> PendingCount(CancellationToken ct)
        => Ok(new { count = await submissionService.CountPendingAsync(ct) });

    /// <summary>
    /// Mentor records a binary decision: Approved or NeedsRevision + optional short note.
    /// No score — "passes or it doesn't" is the only judgment needed.
    /// </summary>
    [HttpPut("{id:guid}/review")]
    [Authorize(Roles = "Mentor,Manager")]
    [ProducesResponseType(typeof(SubmissionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubmissionResponse>> Review(
        Guid id, ReviewSubmissionRequest request, CancellationToken ct)
    {
        var reviewerId = GetUserId();
        if (reviewerId is null) return Unauthorized();

        var result = await submissionService.ReviewAsync(id, reviewerId.Value, request, ct);
        if (result is null)
            return Problem(statusCode: 404, title: "Not Found",
                detail: $"Submission {id} not found.");

        return Ok(result);
    }

    private Guid? GetUserId()
    {
        var value = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                 ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
