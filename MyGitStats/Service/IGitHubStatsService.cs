using MyGitStats.Models;

namespace MyGitStats.Service;

public interface IGitHubStatsService
{
    Task<GitHubStatsResponse> GetUserStatsAsync(GitHubStatsRequest request);
}