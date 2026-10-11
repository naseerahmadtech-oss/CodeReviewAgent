using Microsoft.Extensions.AI;
using OllamaSharp;

namespace CodeReviewAgent
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            //// 1. Set up OllamaApiClient
            //var httpClient = new HttpClient
            //{
            //    BaseAddress = new Uri("http://localhost:11434"),
            //    Timeout = TimeSpan.FromMinutes(10)
            //};

            //// 2. Wrap the base client with automatic function invocation
            //IChatClient client = new ChatClientBuilder(
            //        new OllamaApiClient(httpClient, "qwen3:4b"))
            //    .UseFunctionInvocation()
            //    .Build();

            //// 3. Define your tool (the C# method)
            //AIFunction timeFunction = AIFunctionFactory.Create(
            //    TimeTools.GetCurrentTime,
            //    name: "get_current_time",
            //    description: "Gets the current date and time."
            //);

            //// 4. Add the tool to ChatOptions
            //ChatOptions options = new()
            //{
            //    Tools = [timeFunction],
            //    Instructions = "You are a helpful assistant. Use the get_current_time tool to tell the current time."
            //};

            //Console.WriteLine("Ask a question (e.g., 'What time is it?'):");
            //string? userInput = Console.ReadLine();

            //if (!string.IsNullOrWhiteSpace(userInput))
            //{
            //    // 5. Just call GetResponseAsync - the middleware handles the tool loop!
            //    var response = await client.GetResponseAsync(userInput, options);
            //    Console.WriteLine($"\nAI: {response.Text}");
            //}

            var httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:11434"),
                Timeout = TimeSpan.FromMinutes(30)
            };

            IChatClient client = new ChatClientBuilder(
                    new OllamaApiClient(httpClient, "qwen3:4b"))
                .UseFunctionInvocation()
                .Build();

            // Register both tools
            AIFunction timeFunction = AIFunctionFactory.Create(TimeTools.GetCurrentTime);
            AIFunction prFunction = AIFunctionFactory.Create(GitHubTools.GetPrDiff);

            ChatOptions options = new()
            {
                Tools = [timeFunction, prFunction],
                Instructions = "You are a helpful assistant. Use get_current_time for time questions. Use get_pr_diff when the user provides a GitHub PR URL and wants to know about its changes."
            };

            Console.WriteLine("Ask a question (e.g., 'What time is it?' or paste a PR URL):");
            string? userInput = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(userInput))
            {
                var response = await client.GetResponseAsync(userInput, options);
                Console.WriteLine($"\nAI: {response.Text}");
            }
        }
    }
}
