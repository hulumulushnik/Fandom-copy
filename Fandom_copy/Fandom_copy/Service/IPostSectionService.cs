using Fandom_copy.DTOs.Posts;

namespace Fandom_copy.Services
{
    public interface IPostSectionService
    {
        Task<ServiceResult<PostSectionDto>> GetByIdAsync(Guid id);
        Task<ServiceResult<List<PostSectionDto>>> GetByPostIdAsync(Guid postId);
        Task<ServiceResult<PostSectionDto>> CreateAsync(CreatePostSectionDto dto, Guid currentUserId);
        Task<ServiceResult<PostSectionDto>> UpdateAsync(Guid id, UpdatePostSectionDto dto, Guid currentUserId);
        Task<ServiceResult> DeleteAsync(Guid id, Guid currentUserId);
    }
}
