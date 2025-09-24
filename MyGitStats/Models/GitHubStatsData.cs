namespace MyGitStats.Models;

public class GitHubStatsData
{
    public string Username { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int TotalCommits { get; set; }
    public int TotalPullRequests { get; set; }
    public int DayStreak { get; set; }
    public int TotalContributions { get; set; }
    public int PublicRepos { get; set; }
    public int Followers { get; set; }
    public int Following { get; set; }
    public Dictionary<string, int> ContributionsByRepo { get; set; } = new();
    public List<string> RecentActivity { get; set; } = new();
}