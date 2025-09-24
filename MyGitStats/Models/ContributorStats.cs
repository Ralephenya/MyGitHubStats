using System.Text.Json.Serialization;

namespace MyGitStats.Models;

public class ContributorStats
{
    [JsonPropertyName("author")]
    public GitHubUser Author { get; set; } = new();
    
    [JsonPropertyName("total")]
    public int Total { get; set; }
    
    [JsonPropertyName("weeks")]
    public List<WeeklyStats> Weeks { get; set; } = new();
}