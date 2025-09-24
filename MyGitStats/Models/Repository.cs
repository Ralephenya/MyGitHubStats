using System.Text.Json.Serialization;

namespace MyGitStats.Models;

public class Repository
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("full_name")]
    public string FullName { get; set; } = string.Empty;
    
    [JsonPropertyName("private")]
    public bool IsPrivate { get; set; }
}