using Microsoft.EntityFrameworkCore;
using Trail.Api.DTOs.Trails;
using Trail.Api.Infrastructure.Data;

namespace Trail.Api.Application.Services;

public class TrailService(AppDbContext db)
{
    public async Task<IReadOnlyList<TrailResponse>> ListAsync(TrailListQuery query, CancellationToken ct = default)
    {
        query ??= new TrailListQuery();

        var trails = db.Trails.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            trails = trails.Where(t => t.Name.Contains(term) || t.Description.Contains(term));
        }

        trails = trails.OrderBy(t => t.Name);

        if (query.Page is not null || query.PageSize is not null)
        {
            const int defaultPageSize = 50;
            var page = query.Page ?? 1;
            var pageSize = query.PageSize ?? defaultPageSize;

            trails = trails.Skip((page - 1) * pageSize).Take(pageSize);
        }

        return await trails
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
