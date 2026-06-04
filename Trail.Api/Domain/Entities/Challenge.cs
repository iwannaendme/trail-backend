namespace Trail.Api.Domain.Entities;

public class Challenge
{
    public Guid Id { get; set; }
    public Guid TrailId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Trail Trail { get; set; } = null!;
    public ICollection<Submission> Submissions { get; set; } = [];
}
