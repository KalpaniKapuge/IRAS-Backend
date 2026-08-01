// Throwaway verification: calls the real AiServiceClient against the live ai-service
// (must be running at http://127.0.0.1:8000 — `uvicorn app.main:app --reload`)
// to prove the trained fit-classifier model is genuinely reachable end-to-end
// from the actual C# code path, not just from an isolated Python request.
using IRAS.Application.Common.Ai;
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<AiServiceClient>();

using var http = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:8000") };
var client = new AiServiceClient(http, logger);

var candidates = new List<RankCandidateInput>
{
    new(1, "Experienced Python developer with 3 years working on AWS cloud infrastructure and Docker deployments."),
    new(2, "Retail sales associate with 5 years of experience in customer service and inventory management."),
    new(3, "Full-stack software engineer skilled in React, Node.js, and PostgreSQL, 4 years of experience.")
};

Console.WriteLine("Calling AiServiceClient.RankAsync against the live ai-service...\n");

var result = await client.RankAsync(
    "Looking for a software engineer with Python and AWS experience.",
    candidates,
    CancellationToken.None);

Console.WriteLine($"Success: {result.Success}");
Console.WriteLine($"Error: {result.Error ?? "(none)"}");
Console.WriteLine();

foreach (var r in result.Results)
{
    Console.WriteLine($"Candidate {r.CandidateId}: SemanticSimilarity={r.SemanticSimilarity:F3}, " +
                       $"FitLabel={r.FitLabel ?? "(null)"}, FitScore={r.FitScore?.ToString("F3") ?? "(null)"}");
}
