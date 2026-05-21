using Microsoft.EntityFrameworkCore;
using Trail.Api.DTOs.Trails;
using Trail.Api.Infrastructure.Data;

namespace Trail.Api.Application.Services;

public class TrailService(AppDbContext db)
{
    public async Task<IReadOnlyList<TrailResponse>> ListAsync(CancellationToken ct = default)
    {
        return await db.Trails
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new TrailResponse(t.Id, t.Name, t.Description, t.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ChallengeResponse>?> GetChallengesAsync(Guid trailId, CancellationToken ct = default)
    {
        var trailExists = await db.Trails.AnyAsync(t => t.Id == trailId, ct);
        if (!trailExists) return null;

        return await db.Challenges
            .AsNoTracking()
            .Where(c => c.TrailId == trailId)
            .OrderBy(c => c.Order)
            .Select(c => new ChallengeResponse(c.Id, c.TrailId, c.Title, c.Description, c.Order, c.CreatedAt))
            .ToListAsync(ct);
    }
}
