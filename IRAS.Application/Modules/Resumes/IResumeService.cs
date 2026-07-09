// IRAS.Application/Modules/Resumes/IResumeService.cs
using Microsoft.AspNetCore.Http;
using IRAS.Application.Modules.Resumes.DTOs;

namespace IRAS.Application.Modules.Resumes
{
    public interface IResumeService
    {
        Task<List<ResumeDto>> GetMyResumesAsync(int candidateId, CancellationToken ct);
        Task<ParseResultDto> UploadAndParseAsync(int candidateId, IFormFile file, CancellationToken ct);
        Task<ParseResultDto> RetryParseAsync(int candidateId, int resumeId, CancellationToken ct);
        Task ConfirmSkillsAsync(int candidateId, int resumeId, ConfirmSkillsRequest request, CancellationToken ct);
        Task SetPrimaryAsync(int candidateId, int resumeId, CancellationToken ct);
        Task DeleteAsync(int candidateId, int resumeId, CancellationToken ct);
    }
}
