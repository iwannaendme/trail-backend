using System.ComponentModel.DataAnnotations;

namespace Trail.Api.Configuration;

public class AnthropicOptions
{
    public const string SectionName = "Anthropic";

    [Required]
    public string ApiKey { get; init; } = string.Empty;

    public string Model { get; init; } = "claude-sonnet-4-6";

    /// <summary>Max output tokens. 8 192 covers a full trail with 10 challenges.</summary>
    public int MaxTokens { get; init; } = 8192;
}
