using Microsoft.AspNetCore.Http;

namespace PIPDC.Application.Services;

public interface IImageService
{
    Task<ImageUploadResult> UploadAsync(IFormFile file, string folder, CancellationToken ct = default);
    Task DeleteAsync(string publicId, CancellationToken ct = default);
}

public record ImageUploadResult(string Url, string PublicId);
