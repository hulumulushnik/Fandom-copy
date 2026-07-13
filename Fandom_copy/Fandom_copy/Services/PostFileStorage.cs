using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Fandom_copy.Services;

public sealed class PostFileStorage : IPostFileStorage
{
    private const long MaxFileBytes = 15 * 1024 * 1024;
    private static readonly HashSet<string> AllowedFileExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".txt", ".zip", ".docx", ".xlsx", ".png", ".jpg"
    };

    private readonly IWebHostEnvironment _environment;

    public PostFileStorage(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<FileSaveResult> SaveAsync(Guid postId, IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return FileSaveResult.Fail("Виберіть файл.");

        if (file.Length > MaxFileBytes)
            return FileSaveResult.Fail("Файл занадто великий. Максимум — 15 МБ.");

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedFileExtensions.Contains(extension))
            return FileSaveResult.Fail("Підтримуються тільки PDF, TXT, ZIP, DOCX, XLSX, PNG та JPG.");

        var webRoot = GetWebRoot();
        var relativeFolder = Path.Combine("uploads", "posts", postId.ToString("N"), "files");
        var absoluteFolder = Path.Combine(webRoot, relativeFolder);
        Directory.CreateDirectory(absoluteFolder);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var absolutePath = Path.Combine(absoluteFolder, fileName);
        await using (var stream = File.Create(absolutePath))
            await file.CopyToAsync(stream);

        var relativePath = "/" + Path.Combine(relativeFolder, fileName).Replace('\\', '/');
        return FileSaveResult.Ok(relativePath);
    }

    public void Delete(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) ||
            !relativePath.StartsWith("/uploads/posts/", StringComparison.OrdinalIgnoreCase) ||
            !relativePath.Contains("/files/", StringComparison.OrdinalIgnoreCase))
            return;

        var webRoot = GetWebRoot();
        var fullPath = Path.GetFullPath(Path.Combine(webRoot, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
        var filesRoot = Path.GetFullPath(Path.Combine(webRoot, "uploads", "posts"));

        if (!fullPath.StartsWith(filesRoot, StringComparison.OrdinalIgnoreCase))
            return;

        if (File.Exists(fullPath))
            File.Delete(fullPath);
    }

    private string GetWebRoot()
    {
        if (!string.IsNullOrWhiteSpace(_environment.WebRootPath))
            return _environment.WebRootPath;

        return Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
    }
}
