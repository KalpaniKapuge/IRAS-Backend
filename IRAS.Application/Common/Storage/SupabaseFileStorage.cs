using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using IRAS.Application.Common.Options;

namespace IRAS.Application.Common.Storage
{
    public class SupabaseFileStorage : IFileStorage
    {
        private readonly HttpClient _http;
        private readonly FileStorageOptions _options;

        public SupabaseFileStorage(HttpClient http, IOptions<FileStorageOptions> options)
        {
            _http = http;
            _options = options.Value;

            if (string.IsNullOrWhiteSpace(_options.SupabaseUrl) ||
                string.IsNullOrWhiteSpace(_options.SupabaseServiceRoleKey) ||
                string.IsNullOrWhiteSpace(_options.SupabaseBucket))
            {
                throw new InvalidOperationException(
                    "Supabase storage requires FileStorage:SupabaseUrl, SupabaseServiceRoleKey, and SupabaseBucket.");
            }

            _http.BaseAddress = new Uri(_options.SupabaseUrl.TrimEnd('/') + "/");
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.SupabaseServiceRoleKey);
            _http.DefaultRequestHeaders.Add("apikey", _options.SupabaseServiceRoleKey);
        }

        public async Task<string> SaveAsync(Stream content, string relativeFolder, string fileName, CancellationToken ct)
        {
            var objectKey = BuildObjectKey(relativeFolder, fileName);
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"storage/v1/object/{Uri.EscapeDataString(_options.SupabaseBucket!)}/{objectKey}");

            request.Headers.Add("x-upsert", "true");
            request.Content = new StreamContent(content);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(GetContentType(fileName));

            using var response = await _http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            return BuildPublicUrl(objectKey);
        }

        public async Task<Stream> OpenReadAsync(string storedPath, CancellationToken ct)
        {
            var response = await _http.GetAsync(GetReadableUrl(storedPath), ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync(ct);
        }

        public async Task DeleteAsync(string storedPath, CancellationToken ct)
        {
            var objectKey = ExtractObjectKey(storedPath);
            if (string.IsNullOrWhiteSpace(objectKey))
                return;

            using var request = new HttpRequestMessage(
                HttpMethod.Delete,
                $"storage/v1/object/{Uri.EscapeDataString(_options.SupabaseBucket!)}")
            {
                Content = JsonContent.Create(new { prefixes = new[] { objectKey } })
            };

            using var response = await _http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
        }

        private static string BuildObjectKey(string relativeFolder, string fileName)
        {
            var folder = relativeFolder.Replace('\\', '/').Trim('/');
            return $"{folder}/{fileName}";
        }

        private string BuildPublicUrl(string objectKey)
        {
            var baseUrl = string.IsNullOrWhiteSpace(_options.SupabasePublicBaseUrl)
                ? $"{_options.SupabaseUrl!.TrimEnd('/')}/storage/v1/object/public/{_options.SupabaseBucket}"
                : _options.SupabasePublicBaseUrl.TrimEnd('/');

            return $"{baseUrl}/{objectKey}";
        }

        private string GetReadableUrl(string storedPath)
        {
            if (Uri.TryCreate(storedPath, UriKind.Absolute, out _))
                return storedPath;

            return BuildPublicUrl(storedPath.Replace('\\', '/').Trim('/'));
        }

        private string ExtractObjectKey(string storedPath)
        {
            var marker = $"/storage/v1/object/public/{_options.SupabaseBucket}/";
            var markerIndex = storedPath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex >= 0)
                return storedPath[(markerIndex + marker.Length)..];

            if (!string.IsNullOrWhiteSpace(_options.SupabasePublicBaseUrl))
            {
                var publicBase = _options.SupabasePublicBaseUrl.TrimEnd('/') + "/";
                if (storedPath.StartsWith(publicBase, StringComparison.OrdinalIgnoreCase))
                    return storedPath[publicBase.Length..];
            }

            return storedPath.Replace('\\', '/').Trim('/');
        }

        private static string GetContentType(string fileName)
        {
            return Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };
        }
    }
}
