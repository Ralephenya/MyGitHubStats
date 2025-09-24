using System.Text.Json.Serialization;

namespace MyGitStats.Models;

public class WeeklyStats
{
    [JsonPropertyName("w")]
    public long WeekTimestamp { get; set; }
    
    [JsonPropertyName("a")]
    public int Additions { get; set; }
    
    [JsonPropertyName("d")]
    public int Deletions { get; set; }
    
    [JsonPropertyName("c")]
    public int Commits { get; set; }
}