using System.ComponentModel.DataAnnotations;

namespace Trail.Api.DTOs.Submissions;

public record CreateSubmissionRequest(
    [Required] Guid ChallengeId,
    [Required][MaxLength(2048)]
    [RegularExpression(
        @"^https://(github\.com/[\w.\-]+/[\w.\-]+(/(tree|blob|commit|pulls|issues|discussions).*)?|gist\.github\.com/[\w.\-]+/[0-9a-f]+)/?$",
        ErrorMessage = "Must be a valid GitHub repository or Gist URL.")]
    string GitHubUrl
);
