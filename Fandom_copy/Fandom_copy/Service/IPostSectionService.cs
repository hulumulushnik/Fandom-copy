using Fandom_copy.DTOs.Posts;

namespace Fandom_copy.Services
{
    public interface IPostSectionService
    {
        /// <summary>
        /// Отримати підпост з прямими дочірніми (для сторінки-деталей)
        /// разом із хлібними крихтами до кореня.
        /// </summary>
        Task<ServiceResult<PostSectionDto>> GetByIdAsync(Guid id, Guid? currentUserId);

        Task<ServiceResult<List<PostSectionDto>>> GetByPostIdAsync(Guid postId);
        Task<ServiceResult<PostSectionDto>> CreateAsync(CreatePostSectionDto dto, Guid currentUserId);
        Task<ServiceResult<PostSectionDto>> UpdateAsync(Guid id, UpdatePostSectionDto dto, Guid currentUserId);
        Task<ServiceResult> DeleteAsync(Guid id, Guid currentUserId);
    }
}
