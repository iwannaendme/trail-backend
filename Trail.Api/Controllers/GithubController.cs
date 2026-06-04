using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Trail.Api.Controllers;

/// <summary>
/// Server-side proxy to the GitHub API. Keeps GitHub tokens (if any) server-side
/// and avoids CORS issues when the frontend wants to preview repo/gist metadata.
/// </summary>
[ApiController]
[Route("github")]
[Authorize]
public class GithubController(IHttpClientFactory httpClientFactory) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [HttpGet("meta")]
    [ProducesResponseType(typeof(GitHubMetaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<GitHubMetaResponse>> GetMeta(
        [FromQuery][Required] string url, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(url))
            return Problem(statusCode: 400, title: "Bad Request", detail: "url is required.");

        var (type, apiUrl) = ClassifyAndBuildApiUrl(url.Trim());
        if (type is UrlType.Invalid)
            return Problem(statusCode: 400, title: "Bad Request",
                detail: "Not a valid GitHub repository or Gist URL.");

        var client = httpClientFactory.CreateClient("github");
        try
        {
            var response = await client.GetAsync(apiUrl, ct);
            if (!response.IsSuccessStatusCode)
                return Problem(statusCode: 502, title: "Bad Gateway",
                    detail: $"GitHub API returned {(int)response.StatusCode}.");

            var body = await response.Content.ReadAsStringAsync(ct);
            var meta = type == UrlType.Repo
                ? ParseRepo(body)
                : ParseGist(body);

            return Ok(meta);
        }
        catch (HttpRequestException)
        {
            return Problem(statusCode: 502, title: "Bad Gateway",
                detail: "Could not reach GitHub API.");
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private enum UrlType { Repo, Gist, Invalid }

    private static (UrlType type, string apiUrl) ClassifyAndBuildApiUrl(string url)
    {
        // gist.github.com/{user}/{id}
        var gistMatch = System.Text.RegularExpressions.Regex.Match(
            url, @"gist\.github\.com/([\w.\-]+)/([0-9a-f]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (gistMatch.Success)
            return (UrlType.Gist, $"https://api.github.com/gists/{gistMatch.Groups[2].Value}");

        // github.com/{user}/{repo}
        var repoMatch = System.Text.RegularExpressions.Regex.Match(
            url, @"github\.com/([\w.\-]+)/([\w.\-]+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (repoMatch.Success)
            return (UrlType.Repo, $"https://api.github.com/repos/{repoMatch.Groups[1].Value}/{repoMatch.Groups[2].Value}");

        return (UrlType.Invalid, string.Empty);
    }

    private static GitHubMetaResponse ParseRepo(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        return new GitHubMetaResponse(
            Type: "repo",
            Name: root.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
            Description: root.TryGetProperty("description", out var d) ? d.GetString() : null,
            Language: root.TryGetProperty("language", out var l) ? l.GetString() : null,
            StarCount: root.TryGetProperty("stargazers_count", out var s) ? s.GetInt32() : null,
            LastPushed: root.TryGetProperty("pushed_at", out var p) && DateTime.TryParse(p.GetString(), out var dt) ? dt : null,
            OwnerAvatarUrl: root.TryGetProperty("owner", out var o) && o.TryGetProperty("avatar_url", out var av)
                ? av.GetString() : null,
            HtmlUrl: root.TryGetProperty("html_url", out var h) ? h.GetString() : null
        );
    }

    private static GitHubMetaResponse ParseGist(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var fileCount = root.TryGetProperty("files", out var files) ? files.EnumerateObject().Count() : 0;
        return new GitHubMetaResponse(
            Type: "gist",
            Name: $"{fileCount} file{(fileCount != 1 ? "s" : "")}",
            Description: root.TryGetProperty("description", out var d) ? d.GetString() : null,
            Language: null,
            StarCount: null,
            LastPushed: root.TryGetProperty("updated_at", out var p) && DateTime.TryParse(p.GetString(), out var dt) ? dt : null,
            OwnerAvatarUrl: root.TryGetProperty("owner", out var o) && o.TryGetProperty("avatar_url", out var av)
                ? av.GetString() : null,
            HtmlUrl: root.TryGetProperty("html_url", out var h) ? h.GetString() : null
        );
    }
}

public record GitHubMetaResponse(
    string Type,
    string Name,
    string? Description,
    string? Language,
    int? StarCount,
    DateTime? LastPushed,
    string? OwnerAvatarUrl,
    string? HtmlUrl
);
