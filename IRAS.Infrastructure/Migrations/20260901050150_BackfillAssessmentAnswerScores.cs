using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IRAS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BackfillAssessmentAnswerScores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Data-only fix: ScoreFraction was added (replacing the dropped IsCorrect column)
            // in AddQuestionTypesAndGrading with a default of 0, so any answer rows written by
            // that OLD submit flow — from before this migration — got backfilled to 0
            // regardless of whether they were actually correct. Recompute them here from the
            // still-intact SelectedOptionIndex/CorrectOptionIndex columns (FreeText answers
            // didn't exist before this schema change, so this only ever touches MultipleChoice
            // rows — AI-graded FreeText ScoreFraction values are left untouched).
            migrationBuilder.Sql("""
                UPDATE caa
                SET caa.ScoreFraction = CASE WHEN caa.SelectedOptionIndex = aq.CorrectOptionIndex THEN 1.0 ELSE 0.0 END
                FROM [assessments].[CandidateAssessmentAnswers] caa
                JOIN [assessments].[AssessmentQuestions] aq ON aq.AssessmentQuestionId = caa.AssessmentQuestionId
                WHERE aq.QuestionType = 'MultipleChoice' AND caa.SelectedOptionIndex IS NOT NULL;
                """);

            // Recompute each completed attempt's overall Score to match its (now-corrected)
            // per-question ScoreFraction values — same "average across every question in the
            // assessment, unanswered counts as 0" formula AssessmentService.SubmitAsync uses.
            migrationBuilder.Sql("""
                UPDATE caat
                SET caat.Score = agg.AvgScore
                FROM [assessments].[CandidateAssessmentAttempts] caat
                CROSS APPLY (
                    SELECT AVG(sub.ScoreFraction) AS AvgScore
                    FROM (
                        SELECT ISNULL(caa.ScoreFraction, 0) AS ScoreFraction
                        FROM [assessments].[AssessmentQuestions] aq
                        LEFT JOIN [assessments].[CandidateAssessmentAnswers] caa
                            ON caa.AssessmentQuestionId = aq.AssessmentQuestionId AND caa.AttemptId = caat.AttemptId
                        WHERE aq.JobAssessmentId = caat.JobAssessmentId
                    ) sub
                ) agg
                WHERE caat.Status = 'Completed';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Not reversible — the pre-fix ScoreFraction values (defaulted to 0) carried no
            // real information to restore.
        }
    }
}
