using Fandom_copy.DTOs.Posts;
using Fandom_copy.Models;

namespace Fandom_copy.Services
{
    public interface IPostService
    {
        Task<ServiceResult<List<PostDto>>> GetAllAsync(Guid? currentUserId, Guid? categoryId = null);
        Task<ServiceResult<PostDto>> GetByIdAsync(Guid id, Guid? currentUserId);
        Task<ServiceResult<PostDto>> CreateAsync(CreatePostDto dto, Guid authorId);
        Task<ServiceResult<PostDto>> UpdateAsync(Guid id, UpdatePostDto dto, Guid currentUserId);
        Task<ServiceResult> DeleteAsync(Guid id, Guid currentUserId);
        Task<ServiceResult> DeleteAsAdminAsync(Guid id, Guid adminUserId);

        // --- Members ---
        Task<ServiceResult<List<PostMemberDto>>> GetMembersAsync(Guid postId, Guid currentUserId);
        Task<ServiceResult<PostMemberDto>> AddMemberAsync(Guid postId, AddPostMemberDto dto, Guid currentUserId);
        Task<ServiceResult<PostMemberDto>> UpdateMemberRoleAsync(Guid postId, Guid memberId, UpdatePostMemberRoleDto dto, Guid currentUserId);
        Task<ServiceResult> RemoveMemberAsync(Guid postId, Guid memberId, Guid currentUserId);

        // --- User lookup (для пошуку користувачів при додаванні едитора) ---
        Task<ServiceResult<List<UserSearchResultDto>>> SearchUsersAsync(Guid postId, string query, Guid currentUserId, int take = 10);
    }
}
