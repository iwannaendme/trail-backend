using Microsoft.EntityFrameworkCore;
using Trail.Api.Domain.Entities;
using Trail.Api.Domain.Enums;
using Trail.Api.DTOs.Metrics;
using Trail.Api.DTOs.Students;
using Trail.Api.DTOs.Submissions;
using Trail.Api.Infrastructure.Data;

namespace Trail.Api.Application.Services;

public class SubmissionService(AppDbContext db)
{
    // ── Student actions ───────────────────────────────────────────────────────

    public async Task<SubmissionResponse?> CreateAsync(
        Guid studentId, CreateSubmissionRequest request, CancellationToken ct = default)
    {
        var studentExists = await db.Users
            .AnyAsync(u => u.Id == studentId && u.Role == UserRole.Student, ct);
        if (!studentExists) return null;

        var challenge = await db.Challenges
            .Include(c => c.Trail)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ChallengeId, ct);
        if (challenge is null) return null;

        var submission = new Submission
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            ChallengeId = request.ChallengeId,
            GitHubUrl = request.GitHubUrl,
            SubmittedAt = DateTime.UtcNow,
            Status = SubmissionStatus.Submitted,
        };

        db.Submissions.Add(submission);
        await db.SaveChangesAsync(ct);

        return await GetByIdAsync(submission.Id, ct);
    }

    // ── Mentor actions ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all pending submissions (oldest first — fair queue semantics).
    /// Includes trail name so the mentor can contextualise without opening the link.
    /// </summary>
    public async Task<IReadOnlyList<SubmissionResponse>> ListPendingAsync(CancellationToken ct = default)
        => await db.Submissions
            .AsNoTracking()
            .Where(s => s.Status == SubmissionStatus.Submitted)
            .OrderBy(s => s.SubmittedAt)   // oldest first — respect queue order
            .Select(s => new SubmissionResponse(
                s.Id,
                s.StudentId,
                s.Student.Name,
                s.ChallengeId,
                s.Challenge.Title,
                s.Challenge.Trail.Name,
                s.GitHubUrl,
                s.SubmittedAt,
                s.Status.ToString(),
                s.ReviewerId,
                s.Reviewer != null ? s.Reviewer.Name : null,
                s.MentorComment,
                s.ReviewedAt))
            .ToListAsync(ct);

    /// <summary>
    /// Records a binary review decision. Replaces the 0–100 score system —
    /// a decision is either Approved or NeedsRevision, plus an optional note.
    /// </summary>
    public async Task<SubmissionResponse?> ReviewAsync(
        Guid submissionId, Guid reviewerId, ReviewSubmissionRequest request, CancellationToken ct = default)
    {
        var submission = await db.Submissions
            .FirstOrDefaultAsync(s => s.Id == submissionId, ct);
        if (submission is null) return null;

        submission.Status = request.Decision == ReviewDecision.Approved
            ? SubmissionStatus.Approved
            : SubmissionStatus.NeedsRevision;

        submission.ReviewerId = reviewerId;
        submission.MentorComment = request.Comment?.Trim();
        submission.ReviewedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return await GetByIdAsync(submissionId, ct);
    }

    /// <summary>Count of submissions awaiting review — used for the sidebar badge.</summary>
    public async Task<int> CountPendingAsync(CancellationToken ct = default)
        => await db.Submissions.CountAsync(s => s.Status == SubmissionStatus.Submitted, ct);

    // ── Progress & metrics ────────────────────────────────────────────────────

    public async Task<StudentProgressResponse?> GetStudentProgressAsync(
        Guid studentId, CancellationToken ct = default)
    {
        var student = await db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == studentId && u.Role == UserRole.Student, ct);
        if (student is null) return null;

        var trails = await db.Trails
            .AsNoTracking()
            .Where(t =>
                t.Enrollments.Any(e => e.UserId == studentId) ||
                t.Challenges.Any(c => c.Submissions.Any(s => s.StudentId == studentId)))
            .Select(t => new
            {
                t.Id,
                t.Name,
                TotalChallenges = t.Challenges.Count,
                // Approved = fully done
                CompletedChallenges = t.Challenges.Count(c =>
                    c.Submissions.Any(s => s.StudentId == studentId && s.Status == SubmissionStatus.Approved)),
                // Submitted but not yet Approved (includes NeedsRevision)
                PendingChallenges = t.Challenges.Count(c =>
                    c.Submissions.Any(s => s.StudentId == studentId && s.Status == SubmissionStatus.Submitted) &&
                    !c.Submissions.Any(s => s.StudentId == studentId && s.Status == SubmissionStatus.Approved)),
                LastSubmissionAt = t.Challenges
                    .SelectMany(c => c.Submissions)
                    .Where(s => s.StudentId == studentId)
                    .Select(s => (DateTime?)s.SubmittedAt)
                    .OrderByDescending(s => s)
                    .FirstOrDefault(),
            })
            .OrderBy(t => t.Name)
            .ToListAsync(ct);

        var total = trails.Sum(t => t.TotalChallenges);
        var completed = trails.Sum(t => t.CompletedChallenges);
        var pending = trails.Sum(t => t.PendingChallenges);

        var rate = total == 0 ? 0m : Math.Round((decimal)completed / total * 100m, 2);

        return new StudentProgressResponse(
            student.Id, student.Name, total, completed, pending, rate,
            trails.Select(t => new TrailProgressItem(
                t.Id, t.Name, t.TotalChallenges, t.CompletedChallenges, t.PendingChallenges,
                t.TotalChallenges == 0 ? 0m : Math.Round((decimal)t.CompletedChallenges / t.TotalChallenges * 100m, 2),
                t.LastSubmissionAt)).ToList());
    }

    public async Task<MetricsOverviewResponse> GetMetricsOverviewAsync(CancellationToken ct = default)
    {
        var totalStudents = await db.Users.CountAsync(u => u.Role == UserRole.Student, ct);
        var totalTrails = await db.Trails.CountAsync(ct);
        var totalChallenges = await db.Challenges.CountAsync(ct);
        var totalSubmissions = await db.Submissions.CountAsync(ct);
        var pending = await db.Submissions.CountAsync(s => s.Status == SubmissionStatus.Submitted, ct);
        var approved = await db.Submissions.CountAsync(s => s.Status == SubmissionStatus.Approved, ct);
        var needsRevision = await db.Submissions.CountAsync(s => s.Status == SubmissionStatus.NeedsRevision, ct);

        var reviewed = approved + needsRevision;
        decimal? approvalRate = reviewed == 0
            ? null
            : Math.Round((decimal)approved / reviewed * 100m, 2);

        var approvedChallenges = await db.Submissions
            .Where(s => s.Status == SubmissionStatus.Approved)
            .Select(s => s.ChallengeId)
            .Distinct()
            .CountAsync(ct);

        var completionRate = totalChallenges == 0
            ? 0m
            : Math.Round((decimal)approvedChallenges / totalChallenges * 100m, 2);

        var leadTimeHours = await db.Submissions
            .Where(s => s.ReviewedAt.HasValue)
            .Select(s => (decimal?)EF.Functions.DateDiffMinute(s.SubmittedAt, s.ReviewedAt!.Value) / 60m)
            .AverageAsync(ct);

        return new MetricsOverviewResponse(
            totalStudents, totalTrails, totalChallenges, totalSubmissions,
            pending, approved, needsRevision, completionRate, approvalRate,
            leadTimeHours is null ? null : Math.Round(leadTimeHours.Value, 2));
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private async Task<SubmissionResponse?> GetByIdAsync(Guid id, CancellationToken ct)
        => await db.Submissions
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new SubmissionResponse(
                s.Id,
                s.StudentId,
                s.Student.Name,
                s.ChallengeId,
                s.Challenge.Title,
                s.Challenge.Trail.Name,
                s.GitHubUrl,
                s.SubmittedAt,
                s.Status.ToString(),
                s.ReviewerId,
                s.Reviewer != null ? s.Reviewer.Name : null,
                s.MentorComment,
                s.ReviewedAt))
            .FirstOrDefaultAsync(ct);
}
