using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Trail.Api.Application.Services;
using Trail.Api.DTOs.Ai;

namespace Trail.Api.Controllers;

/// <summary>
/// AI feature endpoints. All require authentication.
///
/// POST /ai/generate-trail   — Student: 5-question onboarding → AI-generated trail
/// POST /ai/chat             — Student: Socratic tutor for a challenge
/// POST /ai/review/{id}      — Mentor: AI co-pilot draft for a submission
/// </summary>
[ApiController]
[Route("ai")]
[Authorize]
public class AiController(
    TrailGenerationService trailGen,
    SocraticAssistantService socratic,
    GitHubReviewService gitHubReview) : ControllerBase
{
    // ── Trail generation (Student only) ───────────────────────────────────────

    /// <summary>
    /// Accepts 5 onboarding data points, generates a personalised trail via AI,
    /// persists it, auto-enrolls the student, and returns the new trail ID.
    ///
    /// This is a synchronous LLM call (10-30 s). The client must show a
    /// loading state and not time out before the response arrives.
    /// </summary>
    [HttpPost("generate-trail")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(GenerateTrailResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<GenerateTrailResult>> GenerateTrail(
        OnboardingProfileRequest request,
        CancellationToken ct)
    {
        var studentId = GetUserId();
        if (studentId is null) return Unauthorized();

        try
        {
            var result = await trailGen.GenerateAsync(studentId.Value, request, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("API key"))
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "AI Service Unavailable",
                detail: "The AI service is not configured. Contact an administrator.");
        }
    }

    // ── Socratic chat (Student only) ──────────────────────────────────────────

    /// <summary>
    /// Receives the full conversation history + new user message.
    /// Returns the assistant's Socratic reply (no code, only guidance).
    ///
    /// Client manages history state; no server-side session is stored.
    /// </summary>
    [HttpPost("chat")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(SocraticChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<SocraticChatResponse>> Chat(
        SocraticChatRequest request,
        CancellationToken ct)
    {
        var studentId = GetUserId();
        if (studentId is null) return Unauthorized();

        try
        {
            var reply = await socratic.ChatAsync(studentId.Value, request, ct);
            return Ok(reply);
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(statusCode: 404, title: "Not Found", detail: ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("API key"))
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "AI Service Unavailable",
                detail: "The AI service is not configured.");
        }
    }

    // ── AI code review co-pilot (Mentor / Manager) ────────────────────────────

    /// <summary>
    /// Fetches the submitted GitHub repo/Gist content and generates a structured
    /// review DRAFT for the mentor to edit before officially submitting.
    ///
    /// The draft is NOT persisted — it is returned to the client for editing.
    /// </summary>
    [HttpPost("review/{submissionId:guid}")]
    [Authorize(Roles = "Mentor,Manager")]
    [ProducesResponseType(typeof(AiReviewDraft), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AiReviewDraft>> ReviewSubmission(
        Guid submissionId,
        CancellationToken ct)
    {
        try
        {
            var draft = await gitHubReview.GenerateReviewDraftAsync(submissionId, ct);
            return Ok(draft);
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(statusCode: 404, title: "Not Found", detail: ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Only pending"))
        {
            return Problem(statusCode: 409, title: "Conflict", detail: ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("API key"))
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "AI Service Unavailable",
                detail: "The AI service is not configured.");
        }
    }

    private Guid? GetUserId()
    {
        var value = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                 ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
