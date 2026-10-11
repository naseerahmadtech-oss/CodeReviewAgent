using Octokit;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace CodeReviewAgent
{

    public class GitHubTools
    {
        [Description("Fetches the diff for a GitHub Pull Request given its URL. The URL format should be 'https://github.com/{owner}/{repo}/pull/{number}'.")]
        public static async Task<string> GetPrDiff(string prUrl)
        {
            // Parse the PR URL to extract owner, repo, and PR number
            var match = Regex.Match(prUrl, @"github\.com/(?<owner>[^/]+)/(?<repo>[^/]+)/pull/(?<number>\d+)");
            if (!match.Success)
            {
                return "Error: Invalid GitHub PR URL format. Expected: https://github.com/owner/repo/pull/123";
            }

            string owner = match.Groups["owner"].Value;
            string repo = match.Groups["repo"].Value;
            int prNumber = int.Parse(match.Groups["number"].Value);

            // Get token from environment or config (we'll pass it via AdditionalProperties or constructor)
            string? token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            if (string.IsNullOrEmpty(token))
            {
                return "Error: GitHub token not found. Set the GITHUB_TOKEN environment variable.";
            }

            var client = new GitHubClient(new ProductHeaderValue("CodeReviewAgent"))
            {
                Credentials = new Credentials(token)
            };

            try
            {
                // Fetch the raw diff using the Accept header
                var diff = await client.Connection.Get<string>(
                    new Uri($"https://api.github.com/repos/{owner}/{repo}/pulls/{prNumber}"),
                    new Dictionary<string, string> { { "Accept", "application/vnd.github.diff" } },
                    null);

                return diff.Body;
            }
            catch (Exception ex)
            {
                return $"Error fetching PR diff: {ex.Message}";
            }
        }
    }
}
