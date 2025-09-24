using Microsoft.AspNetCore.Mvc;
using MyGitStats.Models;
using MyGitStats.Service;

namespace MyGitStats.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GitHubController(IGitHubStatsService gitHubStatsService, ILogger<GitHubController> logger)
    : ControllerBase
{
    /// <summary>
    /// Get GitHub statistics for a user
    /// </summary>
    /// <param name="request">The request containing username and options</param>
    /// <returns>GitHub statistics</returns>
    [HttpPost("stats")]
    public async Task<ActionResult<GitHubStatsResponse>> GetStats([FromBody] GitHubStatsRequest request)
    {
        logger.LogInformation("Stats request for username: {Username}", request?.Username);

        if (request == null)
        {
            return BadRequest(new GitHubStatsResponse
            {
                Success = false,
                Message = "Request body is required"
            });
        }

        var result = await gitHubStatsService.GetUserStatsAsync(request);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get GitHub statistics for a user (GET method)
    /// </summary>
    /// <param name="username">GitHub username</param>
    /// <param name="maxRepos">Maximum repositories to analyze</param>
    /// <param name="includePrivate">Include private repositories</param>
    /// <returns>GitHub statistics</returns>
    [HttpGet("stats/{username}")]
    public async Task<ActionResult<GitHubStatsResponse>> GetStats(
        string username, 
        [FromQuery] int maxRepos = 10, 
        [FromQuery] bool includePrivate = false)
    {
        logger.LogInformation("GET stats request for username: {Username}", username);

        var request = new GitHubStatsRequest
        {
            Username = username,
            MaxRepositories = maxRepos,
            IncludePrivateRepos = includePrivate
        };

        var result = await gitHubStatsService.GetUserStatsAsync(request);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public ActionResult<object> Health()
    {
        return Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        });
    }
}