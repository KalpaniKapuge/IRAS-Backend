// IRAS.Application/Modules/Jobs/IJdGenerator.cs
using IRAS.Domain.Entities.Jobs;

namespace IRAS.Application.Modules.Jobs
{
    public interface IJdGenerator
    {
        string Name { get; }              // "Template" now, "LLM" later
        bool IsAi { get; }
        Task<string> GenerateAsync(Job job, IEnumerable<(string SkillName, string Importance, int MinYears)> skills,
                                   string companyName, string? companyDescription, string? additionalNotes);
    }
}