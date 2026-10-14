using System.ComponentModel;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CodeReviewAgent
{

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

        [Description("Posts a review comment to a GitHub Pull Request. " +
                 "Use this ONLY after the human has approved the review text. " +
                 "The PR URL format should be 'https://github.com/{owner}/{repo}/pull/{number}'.")]
        public static async Task<string> PostReviewComment(
        [Description("The GitHub PR URL.")] string prUrl,
        [Description("The full review text to post (markdown supported).")] string comment,
        [Description("The review action: 'COMMENT' (default), 'APPROVE', or 'REQUEST_CHANGES'.")] string eventType = "COMMENT")
        {
            // 1. Parse URL
            var match = Regex.Match(prUrl,
                @"github\.com/(?<owner>[^/]+)/(?<repo>[^/]+)/pull/(?<number>\d+)");
            if (!match.Success)
                return "Error: Invalid GitHub PR URL format.";

            string owner = match.Groups["owner"].Value;
            string repo = match.Groups["repo"].Value;
            string prNumber = match.Groups["number"].Value;

            // 2. Validate event type
            string[] validEvents = ["COMMENT", "APPROVE", "REQUEST_CHANGES"];
            if (!validEvents.Contains(eventType.ToUpperInvariant()))
                return $"Error: Invalid event type '{eventType}'. Use COMMENT, APPROVE, or REQUEST_CHANGES.";

            // 3. Build payload
            var payload = new
            {
                body = comment,
                @event = eventType.ToUpperInvariant()
            };

            string json = JsonSerializer.Serialize(payload);

            // 4. Build request
            var request = new HttpRequestMessage(HttpMethod.Post,
                $"https://api.github.com/repos/{owner}/{repo}/pulls/{prNumber}/reviews");
            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            request.Headers.UserAgent.Add(
                new ProductInfoHeaderValue("CodeReviewAgent", "1.0"));
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            // 5. Auth
            string? token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            if (string.IsNullOrEmpty(token))
                return "Error: GITHUB_TOKEN environment variable is not set.";
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // 6. Send
            try
            {
                var response = await _httpClient.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return $"Error: GitHub returned {response.StatusCode}. Details: {responseBody}";

                // Try to extract the HTML URL from the response
                using var doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.TryGetProperty("html_url", out var urlProp))
                    return $"Success: Review posted at {urlProp.GetString()}";

                return "Success: Review posted.";
            }
            catch (Exception ex)
            {
                return $"Error posting review: {ex.Message}";
            }

        }
    }
}
