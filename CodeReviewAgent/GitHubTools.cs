using System.ComponentModel;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace CodeReviewAgent
{

    //public class GitHubTools
    //{
    //    [Description("Fetches the diff for a GitHub Pull Request given its URL. The URL format should be 'https://github.com/{owner}/{repo}/pull/{number}'.")]
    //    public static async Task<string> GetPrDiff(string prUrl)
    //    {
    //        // Parse the PR URL to extract owner, repo, and PR number
    //        var match = Regex.Match(prUrl, @"github\.com/(?<owner>[^/]+)/(?<repo>[^/]+)/pull/(?<number>\d+)");
    //        if (!match.Success)
    //        {
    //            return "Error: Invalid GitHub PR URL format. Expected: https://github.com/owner/repo/pull/123";
    //        }

    //        string owner = match.Groups["owner"].Value;
    //        string repo = match.Groups["repo"].Value;
    //        int prNumber = int.Parse(match.Groups["number"].Value);

    //        // Get token from environment or config (we'll pass it via AdditionalProperties or constructor)
    //        string? token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
    //        if (string.IsNullOrEmpty(token))
    //        {
    //            return "Error: GitHub token not found. Set the GITHUB_TOKEN environment variable.";
    //        }

    //        var client = new GitHubClient(new ProductHeaderValue("CodeReviewAgent"))
    //        {
    //            Credentials = new Credentials(token)
    //        };

    //        try
    //        {
    //            // Fetch the raw diff using the Accept header
    //            var diff = await client.Connection.Get<string>(
    //                new Uri($"https://api.github.com/repos/{owner}/{repo}/pulls/{prNumber}"),
    //                new Dictionary<string, string> { { "Accept", "application/vnd.github.diff" } },
    //                null);

    //            return diff.Body;
    //        }
    //        catch (Exception ex)
    //        {
    //            return $"Error fetching PR diff: {ex.Message}";
    //        }
    //    }
    //}

    public class GitHubTools
    {
        private static readonly HttpClient _httpClient = new();

        [Description("Fetches the diff for a GitHub Pull Request given its URL. The URL format should be 'https://github.com/{owner}/{repo}/pull/{number}'.")]
        public static async Task<string> GetPrDiff(string prUrl)
        {
            // 1. Parse URL
            var match = Regex.Match(prUrl, @"github\.com/(?<owner>[^/]+)/(?<repo>[^/]+)/pull/(?<number>\d+)");
            if (!match.Success)
                return "Error: Invalid GitHub PR URL format.";

            string owner = match.Groups["owner"].Value;
            string repo = match.Groups["repo"].Value;
            string prNumber = match.Groups["number"].Value;

            // 2. Build request with diff Accept header
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://api.github.com/repos/{owner}/{repo}/pulls/{prNumber}");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3.diff"));
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("CodeReviewAgent", "1.0"));

            // 3. Add auth if token is present
            string? token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // 4. Send the request
            try
            {
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    return $"Error: GitHub returned {response.StatusCode}. " +
                           $"If this is a private repo, ensure GITHUB_TOKEN is set.";
                }

                string diff = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(diff))
                    return "Error: PR diff was empty. The PR may have no changes or may be merged.";

                // 5. Truncate to protect the small model's context window
                const int maxLength = 40000;
                if (diff.Length > maxLength)
                {
                    diff = diff.Substring(0, maxLength) +
                           "\n\n[... diff truncated due to length ...]";
                }

                return diff;
            }
            catch (Exception ex)
            {
                return $"Error fetching PR diff: {ex.Message}";
            }
        }
    }


}
