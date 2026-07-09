// IRAS.Application/Common/Scoring/ScoringOptionsValidator.cs
using Microsoft.Extensions.Options;

namespace IRAS.Application.Common.Scoring
{
    // Validated on startup (see Program.cs's ValidateOnStart) so a misconfigured
    // appsettings.json fails the boot instead of silently producing wrong scores.
    public class ScoringOptionsValidator : IValidateOptions<ScoringOptions>
    {
        public ValidateOptionsResult Validate(string? name, ScoringOptions options)
        {
            var sum = options.SkillMatchWeight + options.SemanticSimilarityWeight;
            if (Math.Abs(sum - 1.0m) > 0.001m)
                return ValidateOptionsResult.Fail($"Scoring weights must sum to 1.0 (currently {sum}).");

            if (options.AutoMatchThreshold is < 0 or > 1)
                return ValidateOptionsResult.Fail("AutoMatchThreshold must be between 0 and 1.");

            return ValidateOptionsResult.Success;
        }
    }
}
