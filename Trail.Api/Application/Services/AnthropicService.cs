using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Trail.Api.Configuration;

namespace Trail.Api.Application.Services;

// ── Primitive message types ───────────────────────────────────────────────────

public record AnthropicMessage(string Role, string Content);

public record AnthropicTool(
    string Name,
    string Description,
    JsonObject InputSchema);

// ── Response shapes (internal) ────────────────────────────────────────────────

internal record ContentBlock(string Type, string? Text, string? Name, JsonNode? Input);
internal record MessagesResponse(IReadOnlyList<ContentBlock> Content);

/// <summary>
/// Thin wrapper around the Anthropic Messages API.
///
/// Responsibilities:
///  - Inject API key and version header
///  - Enforce tool_choice to guarantee structured output
///  - Parse tool_use block and deserialise to caller-supplied type T
///  - Offer a raw streaming path for the Socratic chat endpoint
///
/// Does NOT retry. Retry / circuit-breaker policy is the caller's concern.
/// </summary>
public interface IAnthropicService
{
    /// <summary>
    /// Forces the model to call <paramref name="tool"/> and returns its
    /// input payload deserialised as <typeparamref name="T"/>.
    /// Throws <see cref="InvalidOperationException"/> when the model fails to
    /// produce a valid tool_use block.
    /// </summary>
    Task<T> InvokeToolAsync<T>(
        string systemPrompt,
        IReadOnlyList<AnthropicMessage> messages,
        AnthropicTool tool,
        CancellationToken ct = default);

    /// <summary>
    /// Standard (non-streaming) chat completion. Returns the assistant's reply.
    /// </summary>
    Task<string> ChatAsync(
        string systemPrompt,
        IReadOnlyList<AnthropicMessage> messages,
        int maxTokens = 1024,
        CancellationToken ct = default);
}

public sealed class AnthropicService(
    IHttpClientFactory httpClientFactory,
    IOptions<AnthropicOptions> options) : IAnthropicService
{
    private readonly AnthropicOptions _cfg = options.Value;

    private static readonly JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    // ── Public surface ────────────────────────────────────────────────────────

    public async Task<T> InvokeToolAsync<T>(
        string systemPrompt,
        IReadOnlyList<AnthropicMessage> messages,
        AnthropicTool tool,
        CancellationToken ct = default)
    {
        var body = new
        {
            model = _cfg.Model,
            max_tokens = _cfg.MaxTokens,
            system = systemPrompt,
            tools = new[] { BuildToolPayload(tool) },
            tool_choice = new { type = "tool", name = tool.Name },
            messages = messages.Select(m => new { role = m.Role, content = m.Content }),
        };

        var response = await PostAsync(body, ct);

        var toolBlock = response.Content.FirstOrDefault(b => b.Type == "tool_use");
        if (toolBlock?.Input is null)
            throw new InvalidOperationException(
                $"Anthropic did not return a tool_use block for tool '{tool.Name}'.");

        var json = toolBlock.Input.ToJsonString();
        return JsonSerializer.Deserialize<T>(json, _jsonOpts)
               ?? throw new InvalidOperationException("Failed to deserialise tool input.");
    }

    public async Task<string> ChatAsync(
        string systemPrompt,
        IReadOnlyList<AnthropicMessage> messages,
        int maxTokens = 1024,
        CancellationToken ct = default)
    {
        var body = new
        {
            model = _cfg.Model,
            max_tokens = maxTokens,
            system = systemPrompt,
            messages = messages.Select(m => new { role = m.Role, content = m.Content }),
        };

        var response = await PostAsync(body, ct);

        var textBlock = response.Content.FirstOrDefault(b => b.Type == "text");
        return textBlock?.Text ?? string.Empty;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<MessagesResponse> PostAsync(object body, CancellationToken ct)  // internal type is fine here
    {
        if (string.IsNullOrWhiteSpace(_cfg.ApiKey))
            throw new InvalidOperationException(
                "Anthropic API key is not configured. " +
                "Set Anthropic:ApiKey in appsettings.Development.json.");

        var client = httpClientFactory.CreateClient("anthropic");
        var resp = await client.PostAsJsonAsync("messages", body, _jsonOpts, ct);

        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"Anthropic API error {(int)resp.StatusCode}: {err}");
        }

        return await resp.Content.ReadFromJsonAsync<MessagesResponse>(_jsonOpts, ct)
               ?? throw new InvalidOperationException("Empty response from Anthropic.");
    }

    private static object BuildToolPayload(AnthropicTool tool) => new
    {
        name = tool.Name,
        description = tool.Description,
        input_schema = tool.InputSchema,
    };
}
