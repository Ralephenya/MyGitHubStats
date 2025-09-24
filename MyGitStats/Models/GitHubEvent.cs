using System.Text.Json.Serialization;

namespace MyGitStats.Models;

public class GitHubEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;
    
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [JsonPropertyName("repo")]
    public EventRepository Repo { get; set; } = new();
}