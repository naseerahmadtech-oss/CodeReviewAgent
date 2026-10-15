using Microsoft.Extensions.AI;
using OllamaSharp;

namespace CodeReviewAgent
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:11434"),
                Timeout = TimeSpan.FromMinutes(50)
            };

            IChatClient client = new ChatClientBuilder(
                    new OllamaApiClient(httpClient, "qwen3:4b"))
                .UseFunctionInvocation()
                .Build();

            // Registration tools
            AIFunction timeFunction = AIFunctionFactory.Create(TimeTools.GetCurrentTime);
            AIFunction prFunction = AIFunctionFactory.Create(GitHubTools.GetPrDiff);
            AIFunction postReviewFunction = AIFunctionFactory.Create(GitHubTools.PostReviewComment);

            ChatOptions options = new()
            {
                Tools = [timeFunction, prFunction, postReviewFunction],
                Instructions = """
                      You are a senior C# code reviewer.
                     
                      WORKFLOW:
                      1. When the user provides a PR URL, call `get_pr_diff` first.
                      2. Analyze the diff and present your review in markdown.
                      3. Ask the user: "Would you like me to post this review to the PR? (yes/no)"
                      4. ONLY if the user explicitly says "yes", call `post_review_comment` with the review text.
                     
                      NEVER call `post_review_comment` without explicit user confirmation.
                      Use event type "COMMENT" unless the user specifies otherwise.
                      """,
                Temperature = 0.2f
            };

            // ─── 5. Interactive loop ───
            Console.WriteLine("╔══════════════════════════════════════════════╗");
            Console.WriteLine("║        Code Review Agent (.NET 10)           ║");
            Console.WriteLine("║  Ask about time, or paste a GitHub PR URL    ║");
            Console.WriteLine("║  Type 'exit' to quit                         ║");
            Console.WriteLine("╚══════════════════════════════════════════════╝");

            while (true)
            {
                string? userInput = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(userInput)) continue;
                if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

                try
                {
                    if (!string.IsNullOrWhiteSpace(userInput))
                    {
                        var response = await client.GetResponseAsync(userInput, options);
                        Console.WriteLine($"\nAI: {response.Text}");
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n[Error] {ex.Message}");
                    Console.ResetColor();
                }
            }
        }
    }
}
