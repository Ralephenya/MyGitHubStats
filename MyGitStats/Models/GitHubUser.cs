using System.Text.Json.Serialization;

namespace MyGitStats.Models;

public class GitHubUser
{
    [JsonPropertyName("login")]
    public string Login { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("public_repos")]
    public int PublicRepos { get; set; }
    
    [JsonPropertyName("followers")]
    public int Followers { get; set; }
    
    [JsonPropertyName("following")]
    public int Following { get; set; }
}