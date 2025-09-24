using System.Text.Json.Serialization;

namespace MyGitStats.Models;

public class EventRepository
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}