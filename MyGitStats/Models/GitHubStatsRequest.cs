namespace MyGitStats.Models;

public class GitHubStatsRequest
{
    public string Username { get; set; } = string.Empty;
    public string? GitHubToken { get; set; }
    public int MaxRepositories { get; set; } = 10;
    public bool IncludePrivateRepos { get; set; } = false;
}