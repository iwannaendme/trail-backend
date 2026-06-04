using Microsoft.EntityFrameworkCore;
using Trail.Api.Domain.Entities;
using Trail.Api.Domain.Enums;
using Trail.Api.DTOs.Trails;
using Trail.Api.Infrastructure.Data;
using TrailEntity = Trail.Api.Domain.Entities.Trail;

namespace Trail.Api.Application.Services;

public class TrailService(AppDbContext db)
{
    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<TrailResponse>> ListAsync(TrailListQuery query, CancellationToken ct = default)
    {
        query ??= new TrailListQuery();
        var trails = db.Trails.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            trails = trails.Where(t => t.Name.Contains(term) || t.Description.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(query.Level))
            trails = ApplyLevelFilter(trails, query.Level.Trim());

        trails = trails.OrderBy(t => t.Name);

        if (query.Page is not null || query.PerPage is not null)
        {
            const int defaultPageSize = 50;
            var page = query.Page ?? 1;
            var pageSize = query.PerPage ?? defaultPageSize;
            trails = trails.Skip((page - 1) * pageSize).Take(pageSize);
        }

        return await trails
            .Select(t => new TrailResponse(
                t.Id, t.Name, t.Description, t.CreatedAt,
                t.Challenges.Count,
                ResolveLevel(t.Challenges.Count),
                ResolveEstimatedHours(t.Challenges.Count)))
            .ToListAsync(ct);
    }

    public async Task<TrailResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await db.Trails
            .AsNoTracking()
            .Where(t => t.Id == id)
            .Select(t => new TrailResponse(
                t.Id, t.Name, t.Description, t.CreatedAt,
                t.Challenges.Count,
                ResolveLevel(t.Challenges.Count),
                ResolveEstimatedHours(t.Challenges.Count)))
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>
    /// Returns challenges for a trail, enriched with the given student's submission
    /// status so that <see cref="ChallengeResponse.IsCompleted"/> and the submission
    /// timestamps reflect real progress rather than always being false/null.
    /// </summary>
    public async Task<IReadOnlyList<ChallengeResponse>?> GetChallengesAsync(
        Guid trailId, Guid? studentId, CancellationToken ct = default)
    {
        var trailExists = await db.Trails.AnyAsync(t => t.Id == trailId, ct);
        if (!trailExists) return null;

        // Fetch challenges first (simple, always translates cleanly)
        var challenges = await db.Challenges
            .AsNoTracking()
            .Where(c => c.TrailId == trailId)
            .OrderBy(c => c.Order)
            .ToListAsync(ct);

        // When there's a student context, fetch their submissions for this trail in a
        // single query and group in memory — avoids translation issues with subqueries.
        Dictionary<Guid, Submission> latestByChallenge = [];
        if (studentId.HasValue && challenges.Count > 0)
        {
            var ids = challenges.Select(c => c.Id).ToHashSet();
            var submissions = await db.Submissions
                .AsNoTracking()
                .Where(s => s.StudentId == studentId.Value && ids.Contains(s.ChallengeId))
                .OrderByDescending(s => s.SubmittedAt)
                .ToListAsync(ct);

            // Keep only the most recent submission per challenge
            foreach (var sub in submissions)
                latestByChallenge.TryAdd(sub.ChallengeId, sub);
        }

        return challenges.Select(c =>
        {
            latestByChallenge.TryGetValue(c.Id, out var sub);
            return new ChallengeResponse(
                c.Id, c.TrailId, c.Title, c.Description, c.Order, c.CreatedAt,
                sub?.Status == SubmissionStatus.Approved,
                sub?.SubmittedAt,
                sub?.Status.ToString(),
                c.YouTubeUrl,
                sub?.MentorComment);
        }).ToList();
    }

    // ── Enrollment ────────────────────────────────────────────────────────────

    /// <summary>
    /// Enrolls a student in a trail. Idempotent — returns the existing enrollment
    /// if the student is already enrolled.
    /// </summary>
    public async Task<EnrollmentResponse?> EnrollAsync(Guid trailId, Guid studentId, CancellationToken ct = default)
    {
        var trailExists = await db.Trails.AnyAsync(t => t.Id == trailId, ct);
        if (!trailExists) return null;

        var existing = await db.TrailEnrollments
            .FirstOrDefaultAsync(e => e.TrailId == trailId && e.UserId == studentId, ct);

        if (existing is not null)
            return new EnrollmentResponse(existing.Id, trailId, studentId, existing.EnrolledAt, existing.CompletedAt);

        var enrollment = new TrailEnrollment
        {
            Id = Guid.NewGuid(),
            TrailId = trailId,
            UserId = studentId,
            EnrolledAt = DateTime.UtcNow,
        };
        db.TrailEnrollments.Add(enrollment);
        await db.SaveChangesAsync(ct);

        return new EnrollmentResponse(enrollment.Id, trailId, studentId, enrollment.EnrolledAt, null);
    }

    /// <summary>
    /// Returns whether a student is enrolled in a trail.
    /// </summary>
    public async Task<bool> IsEnrolledAsync(Guid trailId, Guid studentId, CancellationToken ct = default)
        => await db.TrailEnrollments.AnyAsync(e => e.TrailId == trailId && e.UserId == studentId, ct);

    // ── Mutations (Manager only) ───────────────────────────────────────────────

    public async Task<TrailResponse> CreateAsync(CreateTrailRequest request, CancellationToken ct = default)
    {
        var trail = new TrailEntity
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
        };
        db.Trails.Add(trail);
        await db.SaveChangesAsync(ct);

        return new TrailResponse(trail.Id, trail.Name, trail.Description, trail.CreatedAt,
            0, ResolveLevel(0), ResolveEstimatedHours(0));
    }

    public async Task<TrailResponse?> UpdateAsync(Guid id, UpdateTrailRequest request, CancellationToken ct = default)
    {
        var trail = await db.Trails.Include(t => t.Challenges).FirstOrDefaultAsync(t => t.Id == id, ct);
        if (trail is null) return null;

        trail.Name = request.Name;
        trail.Description = request.Description;
        await db.SaveChangesAsync(ct);

        return new TrailResponse(trail.Id, trail.Name, trail.Description, trail.CreatedAt,
            trail.Challenges.Count,
            ResolveLevel(trail.Challenges.Count),
            ResolveEstimatedHours(trail.Challenges.Count));
    }

    /// <returns>true = deleted · null = not found</returns>
    public async Task<bool?> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var trail = await db.Trails
            .Include(t => t.Challenges).ThenInclude(c => c.Submissions)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

        if (trail is null) return null;

        // Explicitly remove submissions before removing challenges/trail.
        // SQL Server blocks multi-path cascades (User→Submission and Trail→Challenge→Submission
        // would both cascade to Submission), so we handle it in the application layer.
        foreach (var challenge in trail.Challenges)
            db.Submissions.RemoveRange(challenge.Submissions);

        db.Trails.Remove(trail);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ChallengeResponse?> AddChallengeAsync(
        Guid trailId, CreateChallengeRequest request, CancellationToken ct = default)
    {
        if (!await db.Trails.AnyAsync(t => t.Id == trailId, ct)) return null;

        var challenge = new Challenge
        {
            Id = Guid.NewGuid(),
            TrailId = trailId,
            Title = request.Title,
            Description = request.Description,
            Order = request.Order,
            YouTubeUrl = request.YouTubeUrl?.Trim(),
            CreatedAt = DateTime.UtcNow,
        };
        db.Challenges.Add(challenge);
        await db.SaveChangesAsync(ct);

        return new ChallengeResponse(challenge.Id, challenge.TrailId, challenge.Title,
            challenge.Description, challenge.Order, challenge.CreatedAt,
            false, null, null, challenge.YouTubeUrl, null);
    }

    public async Task<ChallengeResponse?> UpdateChallengeAsync(
        Guid trailId, Guid challengeId, UpdateChallengeRequest request, CancellationToken ct = default)
    {
        var challenge = await db.Challenges
            .FirstOrDefaultAsync(c => c.Id == challengeId && c.TrailId == trailId, ct);
        if (challenge is null) return null;

        challenge.Title = request.Title;
        challenge.Description = request.Description;
        challenge.Order = request.Order;
        challenge.YouTubeUrl = request.YouTubeUrl?.Trim();
        await db.SaveChangesAsync(ct);

        return new ChallengeResponse(challenge.Id, challenge.TrailId, challenge.Title,
            challenge.Description, challenge.Order, challenge.CreatedAt,
            false, null, null, challenge.YouTubeUrl, null);
    }

    /// <returns>true = deleted · null = not found</returns>
    public async Task<bool?> DeleteChallengeAsync(Guid trailId, Guid challengeId, CancellationToken ct = default)
    {
        var challenge = await db.Challenges
            .Include(c => c.Submissions)
            .FirstOrDefaultAsync(c => c.Id == challengeId && c.TrailId == trailId, ct);

        if (challenge is null) return null;

        // Remove submissions first to avoid FK constraint violations.
        db.Submissions.RemoveRange(challenge.Submissions);
        db.Challenges.Remove(challenge);
        await db.SaveChangesAsync(ct);
        return true;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static IQueryable<TrailEntity> ApplyLevelFilter(IQueryable<TrailEntity> q, string level) =>
        level.ToLowerInvariant() switch
        {
            "iniciante" => q.Where(t => t.Challenges.Count <= 3),
            "intermediario" => q.Where(t => t.Challenges.Count > 3 && t.Challenges.Count <= 6),
            "intermediário" => q.Where(t => t.Challenges.Count > 3 && t.Challenges.Count <= 6),
            "avancado" => q.Where(t => t.Challenges.Count > 6),
            "avançado" => q.Where(t => t.Challenges.Count > 6),
            _ => q,
        };

    private static string ResolveLevel(int count) => count switch
    {
        <= 3 => "Iniciante",
        <= 6 => "Intermediário",
        _ => "Avançado",
    };

    private static decimal ResolveEstimatedHours(int count) => Math.Round(count * 1.5m, 1);
}
