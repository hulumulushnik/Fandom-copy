using Microsoft.AspNetCore.Http;

namespace Fandom_copy.Services;

public interface IPostFileStorage
{
    Task<FileSaveResult> SaveAsync(Guid postId, IFormFile? file);
    void Delete(string relativePath);
}

public sealed class FileSaveResult
{
    public bool Success { get; init; }
    public string? RelativePath { get; init; }
    public string? Error { get; init; }

    public static FileSaveResult Ok(string relativePath) => new()
    {
        Success = true,
        RelativePath = relativePath
    };

    public static FileSaveResult Fail(string error) => new()
    {
        Success = false,
        Error = error
    };
}
