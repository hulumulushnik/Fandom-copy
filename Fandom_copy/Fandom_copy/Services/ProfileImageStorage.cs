using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Fandom_copy.Services;

public sealed class ProfileImageStorage : IProfileImageStorage
{
    // Гіфки важать більше за звичайні фото, тому ліміт трохи вищий, ніж для
    // зображень усередині поста.
    private const long MaxImageBytes = 8 * 1024 * 1024;
    private static readonly HashSet<string> AllowedImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp"
    };

    private readonly IWebHostEnvironment _environment;

    public ProfileImageStorage(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public Task<ImageSaveResult> SaveAvatarAsync(Guid userId, IFormFile? file) =>
        SaveAsync(userId, "avatar", file);

    public Task<ImageSaveResult> SaveBackgroundAsync(Guid userId, IFormFile? file) =>
        SaveAsync(userId, "background", file);

    private async Task<ImageSaveResult> SaveAsync(Guid userId, string kind, IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return ImageSaveResult.Fail("Виберіть зображення або гіфку.");

        if (file.Length > MaxImageBytes)
            return ImageSaveResult.Fail("Файл занадто великий. Максимум — 8 МБ.");

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedImageExtensions.Contains(extension))
            return ImageSaveResult.Fail("Підтримуються тільки JPG, PNG, GIF та WEBP.");

        if (!string.IsNullOrWhiteSpace(file.ContentType) &&
            !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return ImageSaveResult.Fail("Файл має бути зображенням або гіфкою.");

        var webRoot = GetWebRoot();
        var relativeFolder = Path.Combine("uploads", "profiles", userId.ToString("N"), kind);
        var absoluteFolder = Path.Combine(webRoot, relativeFolder);
        Directory.CreateDirectory(absoluteFolder);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var absolutePath = Path.Combine(absoluteFolder, fileName);
        await using (var stream = File.Create(absolutePath))
            await file.CopyToAsync(stream);

        var relativePath = "/" + Path.Combine(relativeFolder, fileName).Replace('\\', '/');
        return ImageSaveResult.Ok(relativePath);
    }

    public void Delete(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) ||
            !relativePath.StartsWith("/uploads/profiles/", StringComparison.OrdinalIgnoreCase))
            return;

        var webRoot = GetWebRoot();
        var fullPath = Path.GetFullPath(Path.Combine(webRoot, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
        var uploadsRoot = Path.GetFullPath(Path.Combine(webRoot, "uploads", "profiles"));

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
