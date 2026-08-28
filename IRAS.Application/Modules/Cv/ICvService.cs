// IRAS.Application/Modules/Cv/ICvService.cs
using Microsoft.AspNetCore.Http;
using IRAS.Application.Modules.Cv.DTOs;

namespace IRAS.Application.Modules.Cv
{
    public interface ICvService
    {
        List<CvTemplateDto> GetAvailableTemplates();

        Task<List<CvSummaryDto>> GetMyCvsAsync(int candidateId, CancellationToken ct);

        Task<CvDetailDto> GetCvDetailAsync(int candidateId, int cvId, CancellationToken ct);

        Task<CvDetailDto> CreateCvAsync(int candidateId, CreateCvRequest request, CancellationToken ct);

        Task UpdateCvAsync(int candidateId, int cvId, UpdateCvRequest request, CancellationToken ct);

        Task<CvDetailDto> UploadCvPhotoAsync(int candidateId, int cvId, IFormFile file, CancellationToken ct);

        Task UpdateSectionItemsAsync(int candidateId, int cvId, UpdateCvSectionItemsRequest request, CancellationToken ct);

        Task DeleteCvAsync(int candidateId, int cvId, CancellationToken ct);

        // Renders the CV to a PDF byte stream using the candidate's current profile data —
        // never a stale snapshot, so editing a profile immediately reflects in every CV.
        Task<byte[]> RenderPdfAsync(int candidateId, int cvId, CancellationToken ct);
    }
}
