using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Fandom_copy.Services;

public sealed class PostImageStorage : IPostImageStorage
{
    private const long MaxImageBytes = 5 * 1024 * 1024;
    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp"
    };

    private readonly IWebHostEnvironment _environment;

    public PostImageStorage(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<ImageSaveResult> SaveAsync(Guid postId, IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return ImageSaveResult.Fail("Оберіть зображення.");

        if (file.Length > MaxImageBytes)
            return ImageSaveResult.Fail("Зображення слишком большое. Максимум — 5 МБ.");

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedImageExtensions.Contains(extension))
            return ImageSaveResult.Fail("Поддерживаются только JPG, PNG, GIF и WEBP.");

        if (!string.IsNullOrWhiteSpace(file.ContentType) &&
            !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return ImageSaveResult.Fail("Файл має бути зображенням.");

        var webRoot = GetWebRoot();
        var relativeFolder = Path.Combine("uploads", "posts", postId.ToString("N"));
        var absoluteFolder = Path.Combine(webRoot, relativeFolder);
        Directory.CreateDirectory(absoluteFolder);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var absolutePath = Path.Combine(absoluteFolder, fileName);
        await using (var stream = File.Create(absolutePath))
            await file.CopyToAsync(stream);

        var relativePath = "/" + Path.Combine(relativeFolder, fileName).Replace('\\', '/');
        return ImageSaveResult.Ok(relativePath);
    }

    public void Delete(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) ||
            !relativePath.StartsWith("/uploads/posts/", StringComparison.OrdinalIgnoreCase))
            return;

        var webRoot = GetWebRoot();
        var fullPath = Path.GetFullPath(Path.Combine(webRoot, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
        var uploadsRoot = Path.GetFullPath(Path.Combine(webRoot, "uploads", "posts"));

        if (!fullPath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase))
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
