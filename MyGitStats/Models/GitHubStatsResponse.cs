namespace MyGitStats.Models;

public class GitHubStatsResponse
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public GitHubStatsData? Data { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}