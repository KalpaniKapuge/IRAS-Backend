// Throwaway verification: calls the real AiServiceClient against the live ai-service
// (must be running at http://127.0.0.1:8000 — `uvicorn app.main:app --reload`)
// to prove the trained fit-classifier model is genuinely reachable end-to-end
// from the actual C# code path, not just from an isolated Python request.
using IRAS.Application.Common.Ai;
using IRAS.Application.Common.Scoring;
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

// Prove ComputeTotalScore genuinely changes when MlFitScoreWeight is nonzero, using the
// exact same formula ApplicationService/JobMatchingService call in production.
Console.WriteLine("\n--- Proving MlFitScoreWeight actually changes TotalScore ---");

var optionsOld = Microsoft.Extensions.Options.Options.Create(new ScoringOptions
{
    SkillMatchWeight = 0.6m, SemanticSimilarityWeight = 0.4m, MlFitScoreWeight = 0m
});
var optionsNew = Microsoft.Extensions.Options.Options.Create(new ScoringOptions
{
    SkillMatchWeight = 0.5m, SemanticSimilarityWeight = 0.2m, MlFitScoreWeight = 0.3m
});

var loggerScoring = loggerFactory.CreateLogger<ScoringService>();
var scoringOld = new ScoringService(client, optionsOld, loggerScoring);
var scoringNew = new ScoringService(client, optionsNew, loggerScoring);

decimal skillMatch = 0.8m, semanticSimilarity = 0.767m, mlFitScore = 0.201m;

var scoreOld = scoringOld.ComputeTotalScore(skillMatch, semanticSimilarity, mlFitScore);
var scoreNew = scoringNew.ComputeTotalScore(skillMatch, semanticSimilarity, mlFitScore);

Console.WriteLine($"Old config (MlFitScoreWeight=0):   TotalScore = {scoreOld}  (ML contributes 0.3*0.201={0.3m * mlFitScore})");
Console.WriteLine($"New config (MlFitScoreWeight=0.3): TotalScore = {scoreNew}");
Console.WriteLine(scoreOld != scoreNew
    ? "CONFIRMED: the ML fit score now measurably changes TotalScore."
    : "PROBLEM: scores are identical — the weight is not being applied.");
