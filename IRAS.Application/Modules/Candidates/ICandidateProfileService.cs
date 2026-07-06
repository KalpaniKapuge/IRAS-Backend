// IRAS.Application/Modules/Candidates/ICandidateProfileService.cs
using IRAS.Application.Modules.Candidates.DTOs;

namespace IRAS.Application.Modules.Candidates
{
    public interface ICandidateProfileService
    {
        Task<CandidateProfileDto> GetProfileAsync(int candidateId);
        Task UpdateProfileAsync(int candidateId, UpdateCandidateProfileRequest request);

        Task<EducationDto> AddEducationAsync(int candidateId, EducationDto dto);
        Task UpdateEducationAsync(int candidateId, int educationId, EducationDto dto);
        Task DeleteEducationAsync(int candidateId, int educationId);

        Task<WorkExperienceDto> AddWorkExperienceAsync(int candidateId, WorkExperienceDto dto);
        Task UpdateWorkExperienceAsync(int candidateId, int experienceId, WorkExperienceDto dto);
        Task DeleteWorkExperienceAsync(int candidateId, int experienceId);

        Task<CertificationDto> AddCertificationAsync(int candidateId, CertificationDto dto);
        Task DeleteCertificationAsync(int candidateId, int certificationId);

        Task UpsertSkillAsync(int candidateId, UpsertCandidateSkillRequest request);
        Task RemoveSkillAsync(int candidateId, int skillId);
    }
}