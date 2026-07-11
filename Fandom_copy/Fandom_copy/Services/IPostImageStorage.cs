using Microsoft.AspNetCore.Http;

namespace Fandom_copy.Services;

public interface IPostImageStorage
{
    Task<ImageSaveResult> SaveAsync(Guid postId, IFormFile? file);
    void Delete(string relativePath);
}

public sealed class ImageSaveResult
{
    public bool Success { get; init; }
    public string? RelativePath { get; init; }
    public string? Error { get; init; }

    public static ImageSaveResult Ok(string relativePath) => new()
    {
        Success = true,
        RelativePath = relativePath
    };

    public static ImageSaveResult Fail(string error) => new()
    {
        Success = false,
        Error = error
    };
}
