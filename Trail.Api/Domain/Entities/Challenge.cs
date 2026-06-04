namespace Trail.Api.Domain.Entities;

public class Challenge
{
    public Guid Id { get; set; }
    public Guid TrailId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional YouTube guidance video shown before students attempt the activity.
    /// Must be a valid YouTube URL when provided.
    /// </summary>
    public string? YouTubeUrl { get; set; }

    /// <summary>
    /// JSON array of AI-generated YouTube search terms for self-study.
    /// Example: ["ASP.NET Core minimal API tutorial", "C# dependency injection"]
    /// Stored as a JSON string; deserialized at the service layer.
    /// </summary>
    public string? AiSearchTerms { get; set; }

    public Trail Trail { get; set; } = null!;
    public ICollection<Submission> Submissions { get; set; } = [];
}
