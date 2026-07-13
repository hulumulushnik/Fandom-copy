using Microsoft.AspNetCore.Http;

namespace Fandom_copy.Services;

/// <summary>
/// Зберігає завантажені користувачами зображення профілю: іконку (аватар)
/// та фон профілю. Підтримує як звичайні фото, так і гіфки.
/// </summary>
public interface IProfileImageStorage
{
    Task<ImageSaveResult> SaveAvatarAsync(Guid userId, IFormFile? file);
    Task<ImageSaveResult> SaveBackgroundAsync(Guid userId, IFormFile? file);
    void Delete(string? relativePath);
}
