using System.Net.Http.Headers;
using System.Text.Json;
using MyGitStats.Models;

namespace MyGitStats.Service;

public class GitHubStatsService : IGitHubStatsService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GitHubStatsService> _logger;
    private readonly GitHubConfig _config;

    public GitHubStatsService(HttpClient httpClient, IConfiguration configuration, ILogger<GitHubStatsService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _config = configuration.GetSection("GitHub").Get<GitHubConfig>() ?? new GitHubConfig();
        
        _httpClient.Timeout = TimeSpan.FromSeconds(_config.RequestTimeoutSeconds);
    }

    public async Task<GitHubStatsResponse> GetUserStatsAsync(GitHubStatsRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Username))
            {
                return new GitHubStatsResponse
                {
                    Success = false,
                    Message = "Username is required"
                };
            }

            var token = !string.IsNullOrWhiteSpace(request.GitHubToken) 
                ? request.GitHubToken 
                : _config.DefaultToken;

            if (string.IsNullOrWhiteSpace(token))
            {
                return new GitHubStatsResponse
                {
                    Success = false,
                    Message = "GitHub token is required"
                };
            }

            ConfigureHttpClient(token);

            _logger.LogInformation("Fetching GitHub stats for user: {Username}", request.Username);

            var statsData = new GitHubStatsData { Username = request.Username };

            // Get user info
            var user = await GetUserAsync(request.Username);
            statsData.Name = user.Name;
            statsData.PublicRepos = user.PublicRepos;
            statsData.Followers = user.Followers;
            statsData.Following = user.Following;

            // Get repositories
            var repositories = await GetUserRepositoriesAsync(request.Username);
            var maxRepos = Math.Min(request.MaxRepositories, _config.MaxRepositories);
            var reposToProcess = repositories.Take(maxRepos).ToList();

            _logger.LogInformation("Processing {RepoCount} repositories", reposToProcess.Count);

            // Get commits and PRs
            var totalCommits = 0;
            var totalPRs = 0;
            var contributionsByRepo = new Dictionary<string, int>();

            foreach (var repo in reposToProcess)
            {
                try
                {
                    if (repo.IsPrivate && !request.IncludePrivateRepos)
                        continue;

                    var commits = await GetRepositoryCommitsForUserAsync(repo.FullName.Split('/')[0], repo.Name, request.Username);
                    contributionsByRepo[repo.Name] = commits;
                    totalCommits += commits;

                    var prs = await GetPullRequestsCountAsync(repo.FullName.Split('/')[0], repo.Name, request.Username);
                    totalPRs += prs;

                    if (_config.DelayBetweenRequests > 0)
                        await Task.Delay(_config.DelayBetweenRequests);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing repository {RepoName}", repo.Name);
                    contributionsByRepo[repo.Name] = 0;
                }
            }

            statsData.TotalCommits = totalCommits;
            statsData.TotalPullRequests = totalPRs;
            statsData.ContributionsByRepo = contributionsByRepo.Where(kv => kv.Value > 0).ToDictionary();

            // Get user events for streak and contributions
            var events = await GetUserEventsAsync(request.Username);
            statsData.TotalContributions = events.Count;
            statsData.DayStreak = CalculateDayStreak(events);
            statsData.RecentActivity = events.Take(10)
                .Select(e => $"{e.Type} in {e.Repo.Name} on {e.CreatedAt:yyyy-MM-dd}")
                .ToList();

            return new GitHubStatsResponse
            {
                Success = true,
                Message = "Stats retrieved successfully",
                Data = statsData
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching GitHub stats");
            return new GitHubStatsResponse
            {
                Success = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    private void ConfigureHttpClient(string token)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("GitHubStatsAPI", "1.0"));
        _httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
    }

    private async Task<GitHubUser> GetUserAsync(string username)
    {
        var response = await _httpClient.GetStringAsync($"https://api.github.com/users/{username}");
        return JsonSerializer.Deserialize<GitHubUser>(response) ?? new GitHubUser();
    }

    private async Task<List<Repository>> GetUserRepositoriesAsync(string username)
    {
        var repositories = new List<Repository>();
        var page = 1;
        const int perPage = 100;

        while (repositories.Count < _config.MaxRepositories && page <= 3)
        {
            var response = await _httpClient.GetStringAsync(
                $"https://api.github.com/users/{username}/repos?page={page}&per_page={perPage}&sort=updated");
            
            var repos = JsonSerializer.Deserialize<List<Repository>>(response) ?? new List<Repository>();
            
            if (repos.Count == 0) break;

            repositories.AddRange(repos);
            
            if (repos.Count < perPage) break;
            page++;
        }

        return repositories;
    }

    private async Task<int> GetRepositoryCommitsForUserAsync(string owner, string repo, string username)
    {
        try
        {
            var contributorStatsResponse = await _httpClient.GetAsync($"https://api.github.com/repos/{owner}/{repo}/stats/contributors");
            
            if (contributorStatsResponse.IsSuccessStatusCode)
            {
                var contributorStatsJson = await contributorStatsResponse.Content.ReadAsStringAsync();
                var contributorStats = JsonSerializer.Deserialize<List<ContributorStats>>(contributorStatsJson);
                
                var userStats = contributorStats?.FirstOrDefault(c => 
                    c.Author.Login.Equals(username, StringComparison.OrdinalIgnoreCase));
                
                if (userStats != null)
                    return userStats.Total;
            }

            var response = await _httpClient.GetStringAsync(
                $"https://api.github.com/repos/{owner}/{repo}/commits?author={username}&per_page=100");
            
            var commits = JsonSerializer.Deserialize<List<object>>(response) ?? new List<object>();
            return commits.Count;
        }
        catch
        {
            return 0;
        }
    }

    private async Task<int> GetPullRequestsCountAsync(string owner, string repo, string username)
    {
        try
        {
            // Search for PRs created by the user
            var response = await _httpClient.GetStringAsync(
                $"https://api.github.com/search/issues?q=repo:{owner}/{repo}+type:pr+author:{username}&per_page=100");
            
            var searchResult = JsonSerializer.Deserialize<Dictionary<string, object>>(response);
            if (searchResult != null && searchResult.ContainsKey("total_count"))
            {
                return Convert.ToInt32(searchResult["total_count"]);
            }
            
            return 0;
        }
        catch
        {
            return 0;
        }
    }

    private async Task<List<GitHubEvent>> GetUserEventsAsync(string username)
    {
        try
        {
            var response = await _httpClient.GetStringAsync(
                $"https://api.github.com/users/{username}/events/public?per_page=100");
            
            var events = JsonSerializer.Deserialize<List<GitHubEvent>>(response) ?? new List<GitHubEvent>();
            return events.Where(e => e.Type == "PushEvent" || e.Type == "PullRequestEvent" || 
                                     e.Type == "IssuesEvent" || e.Type == "CreateEvent").ToList();
        }
        catch
        {
            return new List<GitHubEvent>();
        }
    }

    private static int CalculateDayStreak(List<GitHubEvent> events)
    {
        if (!events.Any()) return 0;

        var contributionDays = events
            .Select(e => e.CreatedAt.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToList();

        if (!contributionDays.Any()) return 0;

        var streak = 1;
        var currentDate = DateTime.Today;

        if (!contributionDays.Contains(currentDate) && !contributionDays.Contains(currentDate.AddDays(-1)))
            return 0;

        var expectedDate = contributionDays.Contains(currentDate) ? currentDate.AddDays(-1) : currentDate.AddDays(-2);

        foreach (var day in contributionDays.Skip(contributionDays.Contains(currentDate) ? 1 : 0))
        {
            if (day == expectedDate)
            {
                streak++;
                expectedDate = expectedDate.AddDays(-1);
            }
            else
            {
                break;
            }
        }

        return streak;
    }
}