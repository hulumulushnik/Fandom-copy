using Fandom_copy.DTOs.Posts;
using Fandom_copy.Models;

namespace Fandom_copy.Services
{
    public interface IPostService
    {
        Task<ServiceResult<List<PostDto>>> GetAllAsync(Guid? currentUserId);
        Task<ServiceResult<PostDto>> GetByIdAsync(Guid id, Guid? currentUserId);
        Task<ServiceResult<PostDto>> CreateAsync(CreatePostDto dto, Guid authorId);
        Task<ServiceResult<PostDto>> UpdateAsync(Guid id, UpdatePostDto dto, Guid currentUserId);
        Task<ServiceResult> DeleteAsync(Guid id, Guid currentUserId);
    }
}
