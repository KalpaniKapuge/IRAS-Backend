// IRAS.Application/Modules/Candidates/ICandidateProfileService.cs
using IRAS.Application.Modules.Candidates.DTOs;
using Microsoft.AspNetCore.Http;

namespace IRAS.Application.Modules.Candidates
{
    public interface ICandidateProfileService
    {
        Task<CandidateProfileDto> GetProfileAsync(int candidateId);
        Task UpdateProfileAsync(int candidateId, UpdateCandidateProfileRequest request);
        Task<CandidateProfileDto> UploadProfilePictureAsync(int candidateId, IFormFile file, CancellationToken ct);

        Task<EducationDto> AddEducationAsync(int candidateId, EducationDto dto);
        Task UpdateEducationAsync(int candidateId, int educationId, EducationDto dto);
        Task DeleteEducationAsync(int candidateId, int educationId);

        Task<WorkExperienceDto> AddWorkExperienceAsync(int candidateId, WorkExperienceDto dto);
        Task UpdateWorkExperienceAsync(int candidateId, int experienceId, WorkExperienceDto dto);
        Task DeleteWorkExperienceAsync(int candidateId, int experienceId);

        Task<CertificationDto> AddCertificationAsync(int candidateId, CertificationDto dto);
        Task<CertificationDto> AddCertificationAsync(int candidateId, CertificationUploadRequest request, CancellationToken ct);
        Task<CertificationDto> UploadCertificationFileAsync(int candidateId, int certificationId, IFormFile file, CancellationToken ct);
        Task DeleteCertificationAsync(int candidateId, int certificationId);

        Task UpsertSkillAsync(int candidateId, UpsertCandidateSkillRequest request);
        Task RemoveSkillAsync(int candidateId, int skillId);
    }
}
