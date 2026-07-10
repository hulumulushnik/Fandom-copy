using Fandom_copy.Models;

namespace Fandom_copy.DTOs.Posts
{
    public class PostMemberDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Login { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public PostRole Role { get; set; }
        public DateTime AddedAt { get; set; }

        public static PostMemberDto FromEntity(PostMember member)
        {
            return new PostMemberDto
            {
                Id = member.Id,
                UserId = member.UserId,
                Login = member.User?.Login ?? string.Empty,
                Email = member.User?.Email ?? string.Empty,
                Role = member.Role,
                AddedAt = member.AddedAt
            };
        }
    }

    public class UserSearchResultDto
    {
        public Guid Id { get; set; }
        public string Login { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class AddPostMemberDto
    {
        public Guid UserId { get; set; }
        public PostRole Role { get; set; } = PostRole.Editor;
    }

    public class UpdatePostMemberRoleDto
    {
        public PostRole Role { get; set; }
    }
}
