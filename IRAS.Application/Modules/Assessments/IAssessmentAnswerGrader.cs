// IRAS.Application/Modules/Assessments/IAssessmentAnswerGrader.cs
namespace IRAS.Application.Modules.Assessments
{
    // Grades one FreeText answer against its question's model answer. Same swappable-strategy
    // shape as IAssessmentQuestionGenerator/IEvidenceReviewer — a Gemini-backed implementation
    // plus a deterministic Template fallback.
    public interface IAssessmentAnswerGrader
    {
        string Name { get; }

        // Returns a 0..1 correctness fraction. Must never throw — grading one answer failing
        // must not block scoring the rest of the submission.
        Task<decimal> GradeAsync(string questionText, string modelAnswer, string candidateAnswer, CancellationToken ct);
    }
}
