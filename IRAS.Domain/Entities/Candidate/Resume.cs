// IRAS.Domain/Entities/Candidate/Resume.cs
using IRAS.Domain.Enums;

namespace IRAS.Domain.Entities.Candidate
{
    public class Resume
    {
        public int ResumeId { get; set; }
        public int CandidateId { get; set; }
        public string FileUrl { get; set; } = null!;
        public ResumeFormat FileFormat { get; set; }

        // The original uploaded file's name (e.g. "John_Doe_Resume_2026.pdf"), or a
        // CV-derived name for resumes generated from the CV builder — shown in the UI
        // instead of a generic "PDF Resume" label, so a candidate with several resumes can
        // actually tell them apart. FileUrl itself is a randomized storage path, never a
        // presentable name (see ResumeService.UploadAndParseAsync/CreateFromCvAsync).
        public string? FileName { get; set; }
        public bool IsPrimary { get; set; }
        public string? ParsedText { get; set; }
        public ParseStatus ParseStatus { get; set; } = ParseStatus.Pending;
        public string? ParseError { get; set; }              // correction #7
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Set when this resume was generated from a CV-builder CvDocument rather than
        // uploaded directly — lets the UI label it with the CV's own title instead of a
        // generic "PDF Resume", and distinguishes its provenance from an uploaded file.
        // Nullable and SetNull-on-delete: deleting the source CV should never delete a
        // resume that's already been used on a live Application.
        public int? SourceCvId { get; set; }
        public CvDocument? SourceCv { get; set; }

        public CandidateProfile Candidate { get; set; } = null!;
    }
}