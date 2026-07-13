using Fandom_copy.DTOs.Posts;

namespace Fandom_copy.Services;

public interface IPostVersionService
{
    Task<ServiceResult> CaptureAsync(Guid postId, Guid userId, string action);
    Task<ServiceResult<List<PostVersionDto>>> GetVersionsAsync(Guid postId, Guid userId, int take = 10);
    Task<ServiceResult> RestoreAsync(Guid postId, Guid versionId, Guid userId);
}
