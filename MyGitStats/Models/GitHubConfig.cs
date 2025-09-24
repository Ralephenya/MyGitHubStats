namespace MyGitStats.Models;

public class GitHubConfig
{
    public string DefaultToken { get; set; } = string.Empty;
    public int MaxRepositories { get; set; } = 10;
    public int DelayBetweenRequests { get; set; } = 100;
    public int RequestTimeoutSeconds { get; set; } = 30;
}